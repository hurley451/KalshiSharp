using System.Text.Json.Serialization;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.WebSocket;

/// <summary>
/// Real-time trade update from the WebSocket stream.
/// </summary>
public sealed record TradeUpdate : WebSocketMessage<TradeUpdate.MessageBody>
{
    /// <inheritdoc/>
    public override string Type => "trade";

    public sealed record MessageBody
    {
        /// <summary>
        /// Unique identifier for this trade.
        /// </summary>
        [JsonPropertyName("trade_id")]
        public required string TradeId { get; init; }

        /// <summary>
        /// Market ticker this trade occurred in.
        /// </summary>
        [JsonPropertyName("market_ticker")]
        public required string MarketTicker { get; init; }

        /// <summary>
        /// Side of the trade (Yes or No).
        /// </summary>
        [JsonPropertyName("side")]
        public OrderSide Side { get; init; }

        /// <summary>
        /// Price at which the trade executed (in cents).
        /// </summary>
        [JsonPropertyName("yes_price")]
        public int YesPrice { get; init; }

        /// <summary>
        /// No price (derived from yes price, typically 100 - yes_price).
        /// </summary>
        [JsonPropertyName("no_price")]
        public int NoPrice { get; init; }

        /// <summary>
        /// Number of contracts traded.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; init; }

        /// <summary>YES execution price in dollars.</summary>
        public string? YesPriceDollars { get; init; }

        /// <summary>NO execution price in dollars.</summary>
        public string? NoPriceDollars { get; init; }

        /// <summary>Traded quantity as a fixed-point count.</summary>
        public string? CountFp { get; init; }

        /// <summary>Whether this trade was a block trade.</summary>
        public bool IsBlockTrade { get; init; }

        /// <summary>
        /// Taker side of the trade.
        /// </summary>
        [JsonPropertyName("taker_side")]
        public string? TakerSide { get; init; }

        /// <summary>Canonical outcome side of the taker.</summary>
        public OrderSide? TakerOutcomeSide { get; init; }

        /// <summary>Canonical order-book side of the taker.</summary>
        public OrderBookSide? TakerBookSide { get; init; }

        /// <summary>
        /// When this trade occurred (Unix milliseconds).
        /// </summary>
        [JsonPropertyName("ts")]
        public long? TimeStampMs { get; init; }

        /// <summary>Current timestamp in Unix milliseconds.</summary>
        [JsonPropertyName("ts_ms")]
        public long? TsMs { get; init; }

        /// <summary>
        /// Gets the trade creation time as a DateTimeOffset.
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset? TimeStamp => TsMs.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(TsMs.Value)
            : TimeStampMs.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(TimeStampMs.Value)
                : null;
    }
}
