using KalshiSharp.Models.WebSocket;
using KalshiSharp.WebSockets.Connections;
using KalshiSharp.WebSockets.Subscriptions;

namespace KalshiSharp.WebSockets;

/// <summary>
/// Client for Kalshi WebSocket API providing real-time market data and order updates.
/// </summary>
public interface IKalshiWebSocketClient : IAsyncDisposable
{
    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    ConnectionState State { get; }

    /// <summary>
    /// Gets the stream of incoming WebSocket messages.
    /// </summary>
    /// <remarks>
    /// Messages are dispatched as typed <see cref="WebSocketMessage"/> instances.
    /// Unknown message types are wrapped in <see cref="UnknownMessage"/> for passthrough.
    /// </remarks>
    IAsyncEnumerable<WebSocketMessage> Messages { get; }

    /// <summary>
    /// Raised when the connection state changes.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Connects to the Kalshi WebSocket server and authenticates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when already connected.</exception>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the WebSocket server gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to a WebSocket channel.
    /// </summary>
    /// <param name="subscription">The subscription configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when not connected.</exception>
    Task SubscribeAsync(WebSocketSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from a WebSocket channel.
    /// </summary>
    /// <param name="subscription">The subscription to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when not connected.</exception>
    Task UnsubscribeAsync(WebSocketSubscription subscription, CancellationToken cancellationToken = default);
}
