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
    private readonly List<ActiveSubscriptionState> _activeSubscriptions = [];
    private readonly Dictionary<int, ActiveSubscriptionState> _pendingSubscriptions = [];
    private readonly Dictionary<int, PendingCfUpdate> _pendingCfUpdates = [];
    private readonly Dictionary<int, ActiveSubscriptionState> _subscriptionsByServerId = [];
    private readonly object _subscriptionLock = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    // Resources we own and must dispose (only set when using direct instantiation)
    private readonly bool _ownsConnection;

    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private bool _disposed;
    private int _reconnectAttempt;
    private int _nextCommandId;
    private long _nextCfUpdateSequence;
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
        LogConnecting(uri);

        try
        {
            ResetSessionCommandIds();

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

            LogConnected(uri);
            LogAuthenticated();
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            LogConnectionFailed(uri, ex);
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

        var commandId = AllocateCommandId();
        var command = subscription.ToSubscribeCommand(commandId);
        var json = JsonSerializer.Serialize(command, KalshiJsonOptions.Default);
        var bytes = Encoding.UTF8.GetBytes(json);

        var state = new ActiveSubscriptionState(subscription);
        lock (_subscriptionLock)
        {
            _activeSubscriptions.Add(state);
            _pendingSubscriptions[commandId] = state;
        }

        try
        {
            await SendAsync(bytes, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lock (_subscriptionLock)
            {
                RemoveSubscriptionState(state);
            }

            throw;
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

        if (subscription is CfBenchmarksValueSubscription)
        {
            throw new ArgumentException(
                "CF Benchmarks subscriptions must be unsubscribed with their server-assigned subscription ID.",
                nameof(subscription));
        }

        var command = subscription.ToUnsubscribeCommand(AllocateCommandId());
        var json = JsonSerializer.Serialize(command, KalshiJsonOptions.Default);
        var bytes = Encoding.UTF8.GetBytes(json);

        await SendAsync(bytes, cancellationToken).ConfigureAwait(false);

        lock (_subscriptionLock)
        {
            foreach (var state in _activeSubscriptions
                         .Where(state => state.Subscription.Equals(subscription))
                         .ToArray())
            {
                RemoveSubscriptionState(state);
            }
        }

        LogUnsubscribed(subscription.Channel);
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(int subscriptionId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subscriptionId);
        EnsureAuthenticated();

        var command = WebSocketSubscription.ToUnsubscribeCommand(AllocateCommandId(), subscriptionId);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command, KalshiJsonOptions.Default));
        await SendAsync(bytes, cancellationToken).ConfigureAwait(false);

        lock (_subscriptionLock)
        {
            if (_subscriptionsByServerId.TryGetValue(subscriptionId, out var state))
            {
                RemoveSubscriptionState(state);
            }
        }
    }

    /// <inheritdoc />
    public async Task UpdateSubscriptionAsync(
        int subscriptionId,
        SubscriptionUpdateAction action,
        IReadOnlyList<string>? marketTickers = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subscriptionId);
        EnsureAuthenticated();

        var tickers = marketTickers ?? [];
        if ((action is SubscriptionUpdateAction.AddMarkets or SubscriptionUpdateAction.DeleteMarkets) &&
            (tickers.Count == 0 || tickers.Any(string.IsNullOrWhiteSpace)))
        {
            throw new ArgumentException("Market tickers are required for add and delete actions.", nameof(marketTickers));
        }

        var command = WebSocketSubscription.ToUpdateCommand(
            AllocateCommandId(),
            subscriptionId,
            action,
            tickers);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command, KalshiJsonOptions.Default));
        await SendAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> UpdateCfBenchmarksSubscriptionAsync(
        int subscriptionId,
        CfBenchmarksSubscriptionUpdateAction action,
        IReadOnlyList<string>? indexIds = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subscriptionId);
        EnsureAuthenticated();

        var ids = indexIds?.ToArray();
        if (action == CfBenchmarksSubscriptionUpdateAction.IndexList)
        {
            if (ids is { Length: > 0 })
            {
                throw new ArgumentException("Index-list requests cannot include index identifiers.", nameof(indexIds));
            }

            ids = null;
        }
        else
        {
            CfBenchmarksValueSubscription.ValidateIndexIds(ids ?? [], allowEmpty: false);
        }

        ActiveSubscriptionState state;
        lock (_subscriptionLock)
        {
            if (!_subscriptionsByServerId.TryGetValue(subscriptionId, out state!) ||
                state.Subscription is not CfBenchmarksValueSubscription)
            {
                throw new ArgumentException(
                    "The subscription ID does not identify an active CF Benchmarks subscription.",
                    nameof(subscriptionId));
            }
        }

        var replayUpdate = action == CfBenchmarksSubscriptionUpdateAction.IndexList
            ? null
            : new CfBenchmarksReplayUpdate(action, ids!);
        var commandId = await SendCfBenchmarksUpdateAsync(
            subscriptionId,
            action,
            ids,
            state,
            replayUpdate,
            cancellationToken).ConfigureAwait(false);

        return commandId;
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
                if (message is SubscriptionConfirmation confirmation)
                {
                    await HandleSubscriptionConfirmationAsync(confirmation).ConfigureAwait(false);
                }
                else if (message is OKMessage acknowledgement)
                {
                    HandleCfUpdateResult(acknowledgement.Id, succeeded: true);
                }
                else if (message is ErrorMessageV2 error)
                {
                    HandleCfUpdateResult(error.Id, succeeded: false);
                }

                await _messageChannel.Writer.WriteAsync(message).ConfigureAwait(false);
            }
        }
        catch (JsonException ex)
        {
            LogJsonParseError(ex);

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
        catch (JsonException)
        {
            // Fall through to unknown message handling
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
        lock (_subscriptionLock)
        {
            _pendingCfUpdates.Clear();
        }

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

        ResetSessionCommandIds();
        await _connection.ConnectAsync(uri, headers, cancellationToken).ConfigureAwait(false);

        // Re-subscribe to all active subscriptions
        SubscriptionCommand[] commands;
        lock (_subscriptionLock)
        {
            _pendingSubscriptions.Clear();
            _pendingCfUpdates.Clear();
            _subscriptionsByServerId.Clear();

            commands = new SubscriptionCommand[_activeSubscriptions.Count];
            for (var index = 0; index < _activeSubscriptions.Count; index++)
            {
                var state = _activeSubscriptions[index];
                state.ServerId = null;
                state.ReplayCfUpdatesOnConfirmation = state.CfUpdates.Count > 0;

                var commandId = AllocateCommandId();
                commands[index] = state.Subscription.ToSubscribeCommand(commandId);
                _pendingSubscriptions[commandId] = state;
            }
        }

        foreach (var command in commands)
        {
            var json = JsonSerializer.Serialize(command, KalshiJsonOptions.Default);
            var bytes = Encoding.UTF8.GetBytes(json);
            await SendAsync(bytes, cancellationToken).ConfigureAwait(false);
        }

        // Keep the session unavailable to callers until replay command IDs are allocated and sent.
        _connection.MarkAuthenticated();

        _reconnectAttempt = 0;
        _reconnectPolicy.Reset();

        if (_receiveCts is { IsCancellationRequested: false } receiveCancellation)
        {
            _receiveTask = Task.Run(
                () => ReceiveLoopAsync(receiveCancellation.Token),
                receiveCancellation.Token);
        }

        LogReconnected(commands.Length);
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
            _pendingSubscriptions.Clear();
            _pendingCfUpdates.Clear();
            _subscriptionsByServerId.Clear();
        }
    }

    private async Task<int> SendCfBenchmarksUpdateAsync(
        int subscriptionId,
        CfBenchmarksSubscriptionUpdateAction action,
        IReadOnlyList<string>? indexIds,
        ActiveSubscriptionState? state,
        CfBenchmarksReplayUpdate? replayUpdate,
        CancellationToken cancellationToken)
    {
        var commandId = AllocateCommandId();
        var command = WebSocketSubscription.ToCfBenchmarksUpdateCommand(
            commandId,
            subscriptionId,
            action,
            indexIds);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command, KalshiJsonOptions.Default));

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (state is not null && replayUpdate is not null)
            {
                var updateSequence = Interlocked.Increment(ref _nextCfUpdateSequence);
                lock (_subscriptionLock)
                {
                    _pendingCfUpdates[commandId] = new PendingCfUpdate(
                        state,
                        replayUpdate,
                        updateSequence);
                }
            }

            await _connection.SendAsync(bytes, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lock (_subscriptionLock)
            {
                _pendingCfUpdates.Remove(commandId);
            }

            throw;
        }
        finally
        {
            _sendLock.Release();
        }

        return commandId;
    }

    private async Task HandleSubscriptionConfirmationAsync(SubscriptionConfirmation confirmation)
    {
        CfBenchmarksReplayUpdate[] updates = [];
        var subscriptionId = confirmation.Message.Sid;
        if (subscriptionId <= 0)
        {
            return;
        }

        lock (_subscriptionLock)
        {
            if (!_pendingSubscriptions.Remove(confirmation.Id, out var state))
            {
                return;
            }

            state.ServerId = subscriptionId;
            _subscriptionsByServerId[subscriptionId] = state;

            if (state.ReplayCfUpdatesOnConfirmation &&
                state.Subscription is CfBenchmarksValueSubscription)
            {
                updates = state.CfUpdates
                    .OrderBy(update => update.Sequence)
                    .Select(update => update.Update)
                    .ToArray();
                state.ReplayCfUpdatesOnConfirmation = false;
            }
        }

        foreach (var update in updates)
        {
            await SendCfBenchmarksUpdateAsync(
                subscriptionId,
                update.Action,
                update.IndexIds,
                state: null,
                replayUpdate: null,
                cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }
    }

    private void HandleCfUpdateResult(int commandId, bool succeeded)
    {
        lock (_subscriptionLock)
        {
            if (!_pendingCfUpdates.Remove(commandId, out var pending) ||
                !succeeded ||
                !_activeSubscriptions.Contains(pending.State))
            {
                return;
            }

            pending.State.CfUpdates.Add(new ConfirmedCfUpdate(pending.Sequence, pending.Update));
        }
    }

    private int AllocateCommandId()
    {
        var commandId = Interlocked.Increment(ref _nextCommandId);
        return commandId > 0
            ? commandId
            : throw new InvalidOperationException("The WebSocket command identifier range is exhausted.");
    }

    private void ResetSessionCommandIds() => Interlocked.Exchange(ref _nextCommandId, 0);

    private void RemoveSubscriptionState(ActiveSubscriptionState state)
    {
        _activeSubscriptions.Remove(state);

        foreach (var commandId in _pendingSubscriptions
                     .Where(pair => ReferenceEquals(pair.Value, state))
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _pendingSubscriptions.Remove(commandId);
        }

        foreach (var commandId in _pendingCfUpdates
                     .Where(pair => ReferenceEquals(pair.Value.State, state))
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _pendingCfUpdates.Remove(commandId);
        }

        if (state.ServerId is { } serverId &&
            _subscriptionsByServerId.TryGetValue(serverId, out var mapped) &&
            ReferenceEquals(mapped, state))
        {
            _subscriptionsByServerId.Remove(serverId);
        }

        state.ServerId = null;
    }

    private sealed class ActiveSubscriptionState(WebSocketSubscription subscription)
    {
        public WebSocketSubscription Subscription { get; } = subscription;

        public int? ServerId { get; set; }

        public bool ReplayCfUpdatesOnConfirmation { get; set; }

        public List<ConfirmedCfUpdate> CfUpdates { get; } = [];
    }

    private sealed record CfBenchmarksReplayUpdate(
        CfBenchmarksSubscriptionUpdateAction Action,
        IReadOnlyList<string> IndexIds);

    private sealed record PendingCfUpdate(
        ActiveSubscriptionState State,
        CfBenchmarksReplayUpdate Update,
        long Sequence);

    private sealed record ConfirmedCfUpdate(
        long Sequence,
        CfBenchmarksReplayUpdate Update);

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
    private partial void LogConnecting(Uri uri);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket connected to {Uri}")]
    private partial void LogConnected(Uri uri);

    [LoggerMessage(Level = LogLevel.Error, Message = "WebSocket connection failed to {Uri}")]
    private partial void LogConnectionFailed(Uri uri, Exception exception);

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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse WebSocket message")]
    private partial void LogJsonParseError(Exception exception);

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
