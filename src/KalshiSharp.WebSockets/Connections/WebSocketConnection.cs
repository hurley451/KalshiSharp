using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace KalshiSharp.WebSockets.Connections;

/// <summary>
/// Manages a WebSocket connection with state tracking and lifecycle management.
/// </summary>
public sealed partial class WebSocketConnection : IWebSocketConnection
{
    private readonly ILogger<WebSocketConnection> _logger;
    private readonly object _stateLock = new();
    private ClientWebSocket? _webSocket;
    private ConnectionState _state = ConnectionState.Disconnected;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="WebSocketConnection"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public WebSocketConnection(ILogger<WebSocketConnection> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ConnectionState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
    }

    /// <inheritdoc />
    public WebSocketState WebSocketState => _webSocket?.State ?? WebSocketState.None;

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(uri);

        _ = TransitionState(ConnectionState.Connecting);

        try
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();

            LogConnecting(uri.ToString());

            await _webSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);

            TransitionState(ConnectionState.Connected);

            LogConnected(uri.ToString());
        }
        catch (Exception ex)
        {
            LogConnectionFailed(uri.ToString(), ex);
            TransitionState(ConnectionState.Disconnected, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureConnected();

        await _webSocket!.SendAsync(message, WebSocketMessageType.Text, endOfMessage: true, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<WebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureConnected();

        var result = await _webSocket!.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

        // Convert ValueWebSocketReceiveResult to WebSocketReceiveResult
        return new WebSocketReceiveResult(
            result.Count,
            result.MessageType,
            result.EndOfMessage,
            _webSocket.CloseStatus,
            _webSocket.CloseStatusDescription);
    }

    /// <inheritdoc />
    public async Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken = default)
    {
        if (_disposed || _webSocket is null)
        {
            return;
        }

        try
        {
            if (_webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                LogClosing(closeStatus.ToString());

                await _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken)
                    .ConfigureAwait(false);

                LogClosed();
            }
        }
        catch (Exception ex)
        {
            LogCloseError(ex);
        }
        finally
        {
            TransitionState(ConnectionState.Disconnected);
        }
    }

    /// <inheritdoc />
    public void MarkAuthenticated()
    {
        lock (_stateLock)
        {
            if (_state != ConnectionState.Connected)
            {
                throw new InvalidOperationException(
                    $"Cannot mark as authenticated from state {_state}. Expected {ConnectionState.Connected}.");
            }
        }

        TransitionState(ConnectionState.Authenticated);
        LogMarkedAuthenticated();
    }

    /// <inheritdoc />
    public void MarkSubscribed()
    {
        lock (_stateLock)
        {
            if (_state != ConnectionState.Authenticated)
            {
                throw new InvalidOperationException(
                    $"Cannot mark as subscribed from state {_state}. Expected {ConnectionState.Authenticated}.");
            }
        }

        TransitionState(ConnectionState.Subscribed);
        LogMarkedSubscribed();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _webSocket?.Dispose();
        _webSocket = null;
        TransitionState(ConnectionState.Disconnected);
        LogReset();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // Ignore errors during disposal
        }

        _webSocket?.Dispose();
        _webSocket = null;

        LogDisposed();
    }

    private void EnsureConnected()
    {
        if (_webSocket is null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected.");
        }
    }

    private ConnectionState TransitionState(ConnectionState newState, Exception? exception = null)
    {
        ConnectionState previousState;

        lock (_stateLock)
        {
            previousState = _state;
            if (previousState == newState)
            {
                return previousState;
            }

            _state = newState;
        }

        LogStateChanged(previousState.ToString(), newState.ToString());

        StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            PreviousState = previousState,
            NewState = newState,
            Exception = exception
        });

        return previousState;
    }

    // LoggerMessage source-generated logging methods

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connecting to WebSocket at {Uri}")]
    private partial void LogConnecting(string uri);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket connected to {Uri}")]
    private partial void LogConnected(string uri);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to connect to WebSocket at {Uri}")]
    private partial void LogConnectionFailed(string uri, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Closing WebSocket with status {CloseStatus}")]
    private partial void LogClosing(string closeStatus);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket closed gracefully")]
    private partial void LogClosed();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error while closing WebSocket")]
    private partial void LogCloseError(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "WebSocket connection marked as authenticated")]
    private partial void LogMarkedAuthenticated();

    [LoggerMessage(Level = LogLevel.Debug, Message = "WebSocket connection marked as subscribed")]
    private partial void LogMarkedSubscribed();

    [LoggerMessage(Level = LogLevel.Debug, Message = "WebSocket connection reset")]
    private partial void LogReset();

    [LoggerMessage(Level = LogLevel.Debug, Message = "WebSocket connection disposed")]
    private partial void LogDisposed();

    [LoggerMessage(Level = LogLevel.Debug, Message = "WebSocket state changed: {PreviousState} -> {NewState}")]
    private partial void LogStateChanged(string previousState, string newState);
}
