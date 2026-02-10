using System.Diagnostics;
using System.Globalization;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Observability;
using KalshiSharp.Serialization;
using KalshiSharp.Models.WebSocket;
using KalshiSharp.WebSockets.Connections;
using KalshiSharp.WebSockets.ReconnectPolicy;
using KalshiSharp.WebSockets.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KalshiSharp.WebSockets;

/// <summary>
/// WebSocket client for Kalshi real-time market data and order updates.
/// </summary>
public sealed partial class KalshiWebSocketClient : IKalshiWebSocketClient
{
    // Production API serves ALL market types (elections, sports, etc.) despite "elections" in the domain
    private static readonly Uri ProductionWebSocketUri = new("wss://api.elections.kalshi.com/trade-api/ws/v2");
    private static readonly Uri DemoWebSocketUri = new("wss://demo-api.kalshi.co/trade-api/ws/v2");

    private readonly KalshiClientOptions _options;
    private readonly IWebSocketConnection _connection;
    private readonly IReconnectPolicy _reconnectPolicy;
    private readonly ISystemClock _clock;
    private readonly ILogger<KalshiWebSocketClient> _logger;

    private readonly Channel<WebSocketMessage> _messageChannel;
    private readonly HashSet<WebSocketSubscription> _activeSubscriptions = [];
    private readonly object _subscriptionLock = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    // Resources we own and must dispose (only set when using direct instantiation)
    private readonly bool _ownsConnection;

    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private bool _disposed;
    private int _reconnectAttempt;
    private bool _autoReconnect = true;

    /// <summary>
    /// Initializes a new instance of <see cref="KalshiWebSocketClient"/> with the specified options.
    /// This is the recommended constructor for simple usage without dependency injection.
    /// </summary>
    /// <param name="options">The client options containing API credentials and configuration.</param>
    /// <example>
    /// <code>
    /// await using var wsClient = new KalshiWebSocketClient(new KalshiClientOptions
    /// {
    ///     ApiKey = "your-api-key",
    ///     ApiSecret = "-----BEGIN PRIVATE KEY-----\n...",
    ///     Environment = KalshiEnvironment.Production
    /// });
    ///
    /// await wsClient.ConnectAsync();
    /// await wsClient.SubscribeAsync(OrderBookSubscription.ForMarkets("TICKER-ABC"));
    /// </code>
    /// </example>
    public KalshiWebSocketClient(KalshiClientOptions options)
        : this(
            Options.Create(options ?? throw new ArgumentNullException(nameof(options))),
            new WebSocketConnection(NullLogger<WebSocketConnection>.Instance),
            new ExponentialBackoffPolicy(),
            new SystemClock(),
            NullLogger<KalshiWebSocketClient>.Instance)
    {
        _ownsConnection = true;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="KalshiWebSocketClient"/>.
    /// Used when dependencies are provided externally (e.g., via dependency injection).
    /// </summary>
    public KalshiWebSocketClient(
        IOptions<KalshiClientOptions> options,
        IWebSocketConnection connection,
        IReconnectPolicy reconnectPolicy,
        ISystemClock clock,
        ILogger<KalshiWebSocketClient> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _reconnectPolicy = reconnectPolicy ?? throw new ArgumentNullException(nameof(reconnectPolicy));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _messageChannel = Channel.CreateUnbounded<WebSocketMessage>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true
        });

        _connection.StateChanged += OnConnectionStateChanged;
    }

    /// <inheritdoc />
    public ConnectionState State => _connection.State;

    /// <inheritdoc />
    public IAsyncEnumerable<WebSocketMessage> Messages => ReadMessagesAsync();

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connection.State != ConnectionState.Disconnected)
        {
            throw new InvalidOperationException($"Cannot connect from state {_connection.State}. Must be disconnected.");
        }

        using var activity = StartConnectActivity();

        var uri = GetWebSocketUri();
        LogConnecting(uri.ToString());

        try
        {
            // Generate auth headers for WebSocket handshake
            var headers = GenerateAuthHeaders(uri);

            await _connection.ConnectAsync(uri, headers, cancellationToken).ConfigureAwait(false);

            // Mark as authenticated since we authenticated via headers during handshake
            _connection.MarkAuthenticated();

            _reconnectAttempt = 0;
            _reconnectPolicy.Reset();
            _autoReconnect = true;

            // Start receive loop
            _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _receiveTask = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token), _receiveCts.Token);

            LogConnected(uri.ToString());
            LogAuthenticated();
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            LogConnectionFailed(uri.ToString(), ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        _autoReconnect = false;

        // Cancel receive loop
        if (_receiveCts is not null)
        {
            await _receiveCts.CancelAsync().ConfigureAwait(false);
        }

        // Wait for receive task to complete
        if (_receiveTask is not null)
        {
            try
            {
                await _receiveTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                LogReceiveTaskTimeout();
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        // Close connection
        await _connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", cancellationToken)
            .ConfigureAwait(false);

        ClearSubscriptions();

        LogDisconnected();
    }

    /// <inheritdoc />
    public async Task SubscribeAsync(WebSocketSubscription subscription, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(subscription);
        EnsureAuthenticated();

        using var activity = StartSubscribeActivity(subscription.Channel);

        var command = subscription.ToSubscribeCommand();
        var json = JsonSerializer.Serialize(command, KalshiJsonOptions.Default);
        var bytes = Encoding.UTF8.GetBytes(json);

        await SendAsync(bytes, cancellationToken).ConfigureAwait(false);

        lock (_subscriptionLock)
        {
            _activeSubscriptions.Add(subscription);
        }

        LogSubscribed(subscription.Channel, subscription.Markets.Count);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(WebSocketSubscription subscription, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(subscription);
        EnsureAuthenticated();

        var command = subscription.ToUnsubscribeCommand();
        var json = JsonSerializer.Serialize(command, KalshiJsonOptions.Default);
        var bytes = Encoding.UTF8.GetBytes(json);

        await SendAsync(bytes, cancellationToken).ConfigureAwait(false);

        lock (_subscriptionLock)
        {
            _activeSubscriptions.Remove(subscription);
        }

        LogUnsubscribed(subscription.Channel);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _connection.StateChanged -= OnConnectionStateChanged;

        try
        {
            await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Ignore errors during disposal
        }

        _messageChannel.Writer.TryComplete();
        _receiveCts?.Dispose();
        _sendLock.Dispose();

        // Only dispose the connection if we own it (simple constructor was used)
        if (_ownsConnection)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        LogDisposed();
    }

    private async IAsyncEnumerable<WebSocketMessage> ReadMessagesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in _messageChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return message;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var messageBuffer = new MemoryStream();

        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   _connection.WebSocketState == WebSocketState.Open)
            {
                messageBuffer.SetLength(0);

                WebSocketReceiveResult result;
                do
                {
                    result = await _connection.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        LogCloseReceived(result.CloseStatus?.ToString() ?? "Unknown");
                        await HandleDisconnectAsync(cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    messageBuffer.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var messageJson = Encoding.UTF8.GetString(messageBuffer.ToArray());
                    await ProcessMessageAsync(messageJson).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal cancellation
        }
        catch (WebSocketException ex)
        {
            LogWebSocketError(ex);
            await HandleDisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogReceiveError(ex);
            await HandleDisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessMessageAsync(string json)
    {
        try
        {
            var message = ParseMessage(json);
            if (message is not null)
            {
                await _messageChannel.Writer.WriteAsync(message).ConfigureAwait(false);
            }
        }
        catch (JsonException ex)
        {
            LogJsonParseError(json, ex);

            // Create unknown message for unparseable content
            var unknown = new UnknownMessage
            {
                RawType = "parse_error"
            };
            await _messageChannel.Writer.WriteAsync(unknown).ConfigureAwait(false);
        }
    }

    private static WebSocketMessage? ParseMessage(string json)
    {
        // Try polymorphic deserialization first
        try
        {
            var message = JsonSerializer.Deserialize<WebSocketMessage>(json, KalshiJsonOptions.Default);
            if (message is not null)
            {
                return message;
            }
        }
        catch (JsonException ex)
        {
            // Fall through to unknown message handling
            var x = ex;
        }
        catch (Exception ex)
        {
            var x = ex;
        }

        // Parse as JsonElement to extract type for unknown messages
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var type = root.TryGetProperty("type", out var typeProp)
            ? typeProp.GetString() ?? "unknown"
            : "unknown";

        return UnknownMessage.Create(type, root.Clone());
    }

    private async Task HandleDisconnectAsync(CancellationToken cancellationToken)
    {
        _connection.Reset();

        if (!_autoReconnect || _disposed)
        {
            return;
        }

        _reconnectAttempt++;
        var delay = _reconnectPolicy.GetNextDelay(_reconnectAttempt);

        if (delay is null)
        {
            LogMaxReconnectAttemptsReached(_reconnectAttempt);
            return;
        }

        LogReconnecting(_reconnectAttempt, delay.Value.TotalSeconds);

        try
        {
            await Task.Delay(delay.Value, cancellationToken).ConfigureAwait(false);
            await ReconnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancelled during reconnect delay
        }
        catch (Exception ex)
        {
            LogReconnectFailed(ex);
            await HandleDisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        var uri = GetWebSocketUri();
        var headers = GenerateAuthHeaders(uri);

        await _connection.ConnectAsync(uri, headers, cancellationToken).ConfigureAwait(false);
        _connection.MarkAuthenticated();

        // Re-subscribe to all active subscriptions
        WebSocketSubscription[] subscriptions;
        lock (_subscriptionLock)
        {
            subscriptions = [.. _activeSubscriptions];
        }

        foreach (var subscription in subscriptions)
        {
            var command = subscription.ToSubscribeCommand();
            var json = JsonSerializer.Serialize(command, KalshiJsonOptions.Default);
            var bytes = Encoding.UTF8.GetBytes(json);
            await SendAsync(bytes, cancellationToken).ConfigureAwait(false);
        }

        _reconnectAttempt = 0;
        _reconnectPolicy.Reset();

        LogReconnected(subscriptions.Length);
    }

    /// <summary>
    /// Generates authentication headers for the WebSocket handshake using RSA-PSS signing.
    /// </summary>
    private Dictionary<string, string> GenerateAuthHeaders(Uri uri)
    {
        var timestampMs = _clock.UtcNow.ToUnixTimeMilliseconds();
        var timestampStr = timestampMs.ToString(CultureInfo.InvariantCulture);

        // Get path without query for signing
        var path = uri.AbsolutePath;

        // Build message: timestamp + method + path
        var message = timestampStr + "GET" + path;

        // Sign with RSA-PSS
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_options.ApiSecret.AsSpan());

        var messageBytes = Encoding.UTF8.GetBytes(message);
        var signatureBytes = rsa.SignData(messageBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        var signature = Convert.ToBase64String(signatureBytes);

        return new Dictionary<string, string>
        {
            ["KALSHI-ACCESS-KEY"] = _options.ApiKey,
            ["KALSHI-ACCESS-TIMESTAMP"] = timestampStr,
            ["KALSHI-ACCESS-SIGNATURE"] = signature
        };
    }

    private async Task SendAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _connection.SendAsync(bytes, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        StateChanged?.Invoke(this, e);
    }

    private void EnsureAuthenticated()
    {
        var state = _connection.State;
        if (state is not (ConnectionState.Authenticated or ConnectionState.Subscribed))
        {
            throw new InvalidOperationException(
                $"Cannot perform operation in state {state}. Must be authenticated.");
        }
    }

    private void ClearSubscriptions()
    {
        lock (_subscriptionLock)
        {
            _activeSubscriptions.Clear();
        }
    }

    private Uri GetWebSocketUri() => _options.BaseUri is not null
        ? new Uri(_options.BaseUri, "/trade-api/ws/v2")
        : _options.Environment switch
        {
            KalshiEnvironment.Production => ProductionWebSocketUri,
            KalshiEnvironment.Demo => DemoWebSocketUri,
            _ => throw new InvalidOperationException($"Invalid environment: {_options.Environment}")
        };

    private static Activity? StartConnectActivity()
    {
        return KalshiActivitySource.Source.StartActivity(
            KalshiActivitySource.Spans.WebSocketConnect,
            ActivityKind.Client);
    }

    private static Activity? StartSubscribeActivity(string channel)
    {
        var activity = KalshiActivitySource.Source.StartActivity(
            KalshiActivitySource.Spans.WebSocketSubscribe,
            ActivityKind.Client);
        activity?.SetTag("kalshi.ws.channel", channel);
        return activity;
    }

    // Source-generated logging

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connecting to WebSocket at {Uri}")]
    private partial void LogConnecting(string uri);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket connected to {Uri}")]
    private partial void LogConnected(string uri);

    [LoggerMessage(Level = LogLevel.Error, Message = "WebSocket connection failed to {Uri}")]
    private partial void LogConnectionFailed(string uri, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "WebSocket authenticated")]
    private partial void LogAuthenticated();

    [LoggerMessage(Level = LogLevel.Information, Message = "Subscribed to {Channel} for {MarketCount} market(s)")]
    private partial void LogSubscribed(string channel, int marketCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Unsubscribed from {Channel}")]
    private partial void LogUnsubscribed(string channel);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket disconnected")]
    private partial void LogDisconnected();

    [LoggerMessage(Level = LogLevel.Debug, Message = "WebSocket disposed")]
    private partial void LogDisposed();

    [LoggerMessage(Level = LogLevel.Warning, Message = "WebSocket close received: {CloseStatus}")]
    private partial void LogCloseReceived(string closeStatus);

    [LoggerMessage(Level = LogLevel.Warning, Message = "WebSocket error occurred")]
    private partial void LogWebSocketError(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in receive loop")]
    private partial void LogReceiveError(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse WebSocket message: {Json}")]
    private partial void LogJsonParseError(string json, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Attempting reconnect #{Attempt} in {DelaySeconds:F1}s")]
    private partial void LogReconnecting(int attempt, double delaySeconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reconnected successfully, restored {SubscriptionCount} subscription(s)")]
    private partial void LogReconnected(int subscriptionCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Reconnection attempt failed")]
    private partial void LogReconnectFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Max reconnect attempts ({Attempts}) reached")]
    private partial void LogMaxReconnectAttemptsReached(int attempts);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Receive task did not complete within timeout")]
    private partial void LogReceiveTaskTimeout();
}
