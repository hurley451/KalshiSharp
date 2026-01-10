using System.Text.Json.Serialization;

namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>
/// Base class for WebSocket subscription requests.
/// </summary>
public abstract record WebSocketSubscription
{
    /// <summary>
    /// The subscription channel identifier.
    /// </summary>
    [JsonPropertyName("channel")]
    public abstract string Channel { get; }

    /// <summary>
    /// The market tickers to subscribe to.
    /// </summary>
    [JsonPropertyName("markets")]
    public IReadOnlyList<string> Markets { get; init; } = [];

    /// <summary>
    /// Creates a subscription message for sending to the WebSocket server.
    /// </summary>
    /// <returns>The subscription command object.</returns>
    internal SubscriptionCommand ToSubscribeCommand() => new()
    {
        Command = "subscribe",
        Params = new SubscriptionParams
        {
            Channel = Channel,
            Markets = Markets
        }
    };

    /// <summary>
    /// Creates an unsubscription message for sending to the WebSocket server.
    /// </summary>
    /// <returns>The unsubscription command object.</returns>
    internal SubscriptionCommand ToUnsubscribeCommand() => new()
    {
        Command = "unsubscribe",
        Params = new SubscriptionParams
        {
            Channel = Channel,
            Markets = Markets
        }
    };
}

/// <summary>
/// Command sent to the WebSocket server for subscription management.
/// </summary>
internal sealed record SubscriptionCommand
{
    /// <summary>
    /// The command type: "subscribe" or "unsubscribe".
    /// </summary>
    [JsonPropertyName("cmd")]
    public required string Command { get; init; }

    /// <summary>
    /// The subscription parameters.
    /// </summary>
    [JsonPropertyName("params")]
    public required SubscriptionParams Params { get; init; }
}

/// <summary>
/// Parameters for a subscription command.
/// </summary>
internal sealed record SubscriptionParams
{
    /// <summary>
    /// The channel to subscribe to.
    /// </summary>
    [JsonPropertyName("channel")]
    public required string Channel { get; init; }

    /// <summary>
    /// The market tickers to subscribe to.
    /// </summary>
    [JsonPropertyName("markets")]
    public IReadOnlyList<string> Markets { get; init; } = [];
}
