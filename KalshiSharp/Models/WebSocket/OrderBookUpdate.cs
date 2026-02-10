using System.Text.Json.Serialization;

namespace KalshiSharp.Models.WebSocket;

/// <summary>
/// Delta update for an order book. Contains only the changes since the last update.
/// </summary>
public sealed record OrderBookUpdate : WebSocketMessage<OrderBookUpdate.MessageBody>
{
    /// <inheritdoc/>
    public override string Type => "orderbook_delta";    

    /// <summary>
    /// Sequential number that should be checked if you want to guarantee you received all the messages. Used for snapshot/delta consistency
    /// </summary>
    [JsonPropertyName("seq")]
    public int Sequence { get; init; }

    public sealed record MessageBody
    {
        /// <summary>
        /// Market ticker this update is for.
        /// </summary>
        [JsonPropertyName("market_ticker")]
        public required string MarketTicker { get; init; }

        /// <summary>
        /// Unique market UUID.
        /// </summary>
        [JsonPropertyName("market_id")]
        public required string MarketId { get; init; }

        /// <summary>
        /// Price level for this update (1-99 cents).
        /// </summary>
        [JsonPropertyName("price")]
        public required int Price { get; init; }

        /// <summary>
        /// Change in quantity at this price level for Yes side.
        /// Positive = added, negative = removed.
        /// </summary>
        [JsonPropertyName("delta")]
        public required int Delta { get; init; }

        /// <summary>
        /// Side of the order book: "yes" or "no".
        /// </summary>
        [JsonPropertyName("side")]
        public required string Side { get; init; }

        /// <summary>
        /// Contains the client_order_id of your order that triggered this delta.
        /// </summary>
        /// <remarks>
        /// Present only when you caused this orderbook change.
        /// </remarks>
        [JsonPropertyName("client_order_id")]
        public string? ClientOrderId { get; init; }

        /// <summary>
        /// Contains the subaccount number of your order that triggered this delta.
        /// </summary>
        /// <remarks>
        /// Present only when you caused this orderbook change and are using subaccounts.
        /// </remarks>
        [JsonPropertyName("subaccount")]
        public string? Subaccount { get; init; }

        /// <summary>
        /// Timestamp for when the orderbook change was recorded.
        /// </summary>
        [JsonPropertyName("ts")]
        public DateTimeOffset? Timestamp { get; init; }

        /// <summary>
        /// Whether this is the Yes side.
        /// </summary>
        [JsonIgnore]
        public bool IsYesSide => string.Equals(Side, "yes", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Whether this is the No side.
        /// </summary>
        [JsonIgnore]
        public bool IsNoSide => string.Equals(Side, "no", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Full snapshot of an order book. Sent on initial subscription.
/// </summary>
public sealed record OrderBookSnapshot : WebSocketMessage<OrderBookSnapshot.MessageBody>
{
    /// <inheritdoc/>
    public override string Type => "orderbook_snapshot";

    /// <summary>
    /// Sequential number that should be checked if you want to guarantee you received all the messages. Used for snapshot/delta consistency
    /// </summary>
    public int Seq { get; init; }

    public sealed record MessageBody
    {
        /// <summary>
        /// Market ticker this snapshot is for.
        /// </summary>
        [JsonPropertyName("market_ticker")]
        public required string MarketTicker { get; init; }

        /// <summary>
        /// Unique market UUID.
        /// </summary>
        [JsonPropertyName("market_id")]
        public required string MarketId { get; init; }

        /// <summary>
        /// Bids on the Yes side. Each entry is [price, quantity].
        /// </summary>
        [JsonPropertyName("yes")]
        public IReadOnlyList<int[]>? Yes { get; init; }

        /// <summary>
        /// Bids on the Yes side. Each entry is [price, quantity].
        /// Same as "yes" but with price in dollars.
        /// </summary>
        [JsonPropertyName("yes_dollars")]
        public IReadOnlyList<decimal[]>? YesDollars { get; init; }

        /// <summary>
        /// Bids on the Yes side. Each entry is [price, quantity].
        /// Same as "yes_dollars" but with contract counts in fixed-point (2 decimals).
        /// </summary>
        [JsonPropertyName("yes_dollars_fp")]
        public IReadOnlyList<decimal[]>? YesDollarsFp { get; init; }

        /// <summary>
        /// Bids on the No side. Each entry is [price, quantity].
        /// </summary>
        [JsonPropertyName("no")]
        public IReadOnlyList<int[]>? No { get; init; }

        /// <summary>
        /// Bids on the No side. Each entry is [price, quantity].
        /// Same as "no" but with price in dollars.
        /// </summary>
        [JsonPropertyName("no_dollars")]
        public IReadOnlyList<decimal[]>? NoDollars { get; init; }

        /// <summary>
        /// Bids on the No side. Each entry is [price, quantity].
        /// Same as "no_dollars" but with contract counts in fixed-point (2 decimals).
        /// </summary>
        [JsonPropertyName("no_dollars_fp")]
        public IReadOnlyList<decimal[]>? NoDollarsFp { get; init; }
    }
}
