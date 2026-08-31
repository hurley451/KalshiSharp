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
    internal SubscriptionCommand ToSubscribeCommand(int commandId) => new()
    {
        Id = commandId,
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
    internal SubscriptionCommand ToUnsubscribeCommand(int commandId) => new()
    {
        Id = commandId,
        Command = "unsubscribe",
        Params = new SubscriptionParams
        {
            Channels = [Channel],
            MarketTickers = Markets
        }
    };

    /// <summary>Creates an unsubscription command for a server-assigned subscription.</summary>
    internal static SubscriptionCommand ToUnsubscribeCommand(int commandId, int subscriptionId) => new()
    {
        Id = commandId,
        Command = "unsubscribe",
        Params = new SubscriptionParams
        {
            SubscriptionIds = [subscriptionId]
        }
    };

    /// <summary>Creates an update command for an existing subscription.</summary>
    internal static SubscriptionCommand ToUpdateCommand(
        int commandId,
        int subscriptionId,
        SubscriptionUpdateAction action,
        IReadOnlyList<string> marketTickers) => new()
        {
            Id = commandId,
            Command = "update_subscription",
            Params = new SubscriptionParams
            {
                SubscriptionIds = [subscriptionId],
                Action = action switch
                {
                    SubscriptionUpdateAction.AddMarkets => "add_markets",
                    SubscriptionUpdateAction.DeleteMarkets => "delete_markets",
                    SubscriptionUpdateAction.GetSnapshot => "get_snapshot",
                    _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
                },
                MarketTickers = marketTickers
            }
        };

    /// <summary>Creates a CF Benchmarks update command for an existing subscription.</summary>
    internal static SubscriptionCommand ToCfBenchmarksUpdateCommand(
        int commandId,
        int subscriptionId,
        CfBenchmarksSubscriptionUpdateAction action,
        IReadOnlyList<string>? indexIds) => new()
        {
            Id = commandId,
            Command = "update_subscription",
            Params = new SubscriptionParams
            {
                SubscriptionIds = [subscriptionId],
                Action = action switch
                {
                    CfBenchmarksSubscriptionUpdateAction.SubscribeIndices => "subscribe_indices",
                    CfBenchmarksSubscriptionUpdateAction.UnsubscribeIndices => "unsubscribe_indices",
                    CfBenchmarksSubscriptionUpdateAction.IndexList => "indexlist",
                    _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
                },
                IndexIds = indexIds
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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Channels { get; init; }

    /// <summary>
    /// The market tickers to subscribe to.
    /// </summary>
    [JsonPropertyName("market_tickers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? MarketTickers { get; init; }

    /// <summary>
    /// Skips the initial ticker acknowledgement when requested by a ticker subscription.
    /// </summary>
    [JsonPropertyName("skip_ticker_ack")]
    public bool? SkipTickerAck { get; init; }

    /// <summary>Existing subscription identifiers to update.</summary>
    [JsonPropertyName("sids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? SubscriptionIds { get; init; }

    /// <summary>Update action.</summary>
    [JsonPropertyName("action")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Action { get; init; }

    /// <summary>CF Benchmarks index identifiers.</summary>
    [JsonPropertyName("index_ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? IndexIds { get; init; }
}
