using System.Net.WebSockets;

namespace KalshiSharp.WebSockets.Connections;

/// <summary>
/// Abstraction over a WebSocket connection for testability.
/// </summary>
public interface IWebSocketConnection : IAsyncDisposable
{
    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    ConnectionState State { get; }

    /// <summary>
    /// Gets the underlying WebSocket state.
    /// </summary>
    WebSocketState WebSocketState { get; }

    /// <summary>
    /// Raised when the connection state changes.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Connects to the WebSocket server.
    /// </summary>
    /// <param name="uri">The WebSocket URI to connect to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message over the WebSocket.
    /// </summary>
    /// <param name="message">The message bytes to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives a message from the WebSocket.
    /// </summary>
    /// <param name="buffer">Buffer to receive into.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the receive operation.</returns>
    ValueTask<WebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the WebSocket connection gracefully.
    /// </summary>
    /// <param name="closeStatus">The close status.</param>
    /// <param name="statusDescription">Optional status description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions the connection to the authenticated state.
    /// </summary>
    void MarkAuthenticated();

    /// <summary>
    /// Transitions the connection to the subscribed state.
    /// </summary>
    void MarkSubscribed();

    /// <summary>
    /// Resets the connection state to disconnected.
    /// </summary>
    void Reset();
}

/// <summary>
/// Event arguments for connection state changes.
/// </summary>
public sealed class ConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous connection state.
    /// </summary>
    public required ConnectionState PreviousState { get; init; }

    /// <summary>
    /// Gets the new connection state.
    /// </summary>
    public required ConnectionState NewState { get; init; }

    /// <summary>
    /// Gets the exception that caused the state change, if any.
    /// </summary>
    public Exception? Exception { get; init; }
}
