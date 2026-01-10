namespace KalshiSharp.WebSockets.Connections;

/// <summary>
/// Represents the current state of a WebSocket connection.
/// </summary>
/// <remarks>
/// State transitions:
/// <code>
/// Disconnected → Connecting → Connected → Authenticated → Subscribed
///       ↑              |            |             |              |
///       └──────────────┴────────────┴─────────────┴──────────────┘
///                              (on error/close)
/// </code>
/// </remarks>
public enum ConnectionState
{
    /// <summary>
    /// Not connected. Initial state or after disconnect/error.
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Connection attempt in progress.
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// WebSocket connected but not yet authenticated.
    /// </summary>
    Connected = 2,

    /// <summary>
    /// Successfully authenticated with the server.
    /// </summary>
    Authenticated = 3,

    /// <summary>
    /// Authenticated and has active subscriptions.
    /// </summary>
    Subscribed = 4
}
