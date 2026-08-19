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
        Id = 1,
        Command = "subscribe",
        Params = CreateSubscribeParams()
    };

    /// <summary>Creates channel-specific subscription parameters.</summary>
    internal virtual SubscriptionParams CreateSubscribeParams() => new()
    {
        Channels = [Channel],
        MarketTickers = Markets
    };

    /// <summary>
    /// Creates an unsubscription message for sending to the WebSocket server.
    /// </summary>
    /// <returns>The unsubscription command object.</returns>
    internal SubscriptionCommand ToUnsubscribeCommand() => new()
    {
        Id = 2,
        Command = "unsubscribe",
        Params = new SubscriptionParams
        {
            Channels = [Channel],
            MarketTickers = Markets
        }
    };
}

/// <summary>
/// Command sent to the WebSocket server for subscription management.
/// </summary>
internal sealed record SubscriptionCommand
{
    /// <summary>
    /// Message identifier for correlation.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

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
    /// The channels to subscribe to (e.g., "orderbook_delta", "ticker", "trade").
    /// </summary>
    [JsonPropertyName("channels")]
    public required IReadOnlyList<string> Channels { get; init; }

    /// <summary>
    /// The market tickers to subscribe to.
    /// </summary>
    [JsonPropertyName("market_tickers")]
    public IReadOnlyList<string> MarketTickers { get; init; } = [];

    /// <summary>
    /// Skips the initial ticker acknowledgement when requested by a ticker subscription.
    /// </summary>
    [JsonPropertyName("skip_ticker_ack")]
    public bool? SkipTickerAck { get; init; }
}
