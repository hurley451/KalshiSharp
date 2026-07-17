using System.Text.Json.Serialization;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.WebSocket;

/// <summary>
/// Real-time trade update from the WebSocket stream.
/// </summary>
public sealed record TradeUpdate : WebSocketMessage<TradeUpdate.MessageBody>
{
    /// <inheritdoc/>
    public override string MessageType => "trade";

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
        public required OrderSide Side { get; init; }

        /*
        /// <summary>
        /// Price at which the trade executed (in cents).
        /// </summary>
        [JsonPropertyName("yes_price")]
        public required int YesPrice { get; init; }

        /// <summary>
        /// No price (derived from yes price, typically 100 - yes_price).
        /// </summary>
        [JsonPropertyName("no_price")]
        public required int NoPrice { get; init; }

        /// <summary>
        /// Number of contracts traded.
        /// </summary>
        [JsonPropertyName("count")]
        public required int Count { get; init; }
        */

        /// <summary>
        /// Price at which the trade executed in dollars.
        /// </summary>
        [JsonPropertyName("yes_price_dollars")]
        public required string YesPriceDollars { get; init; }

        /// <summary>
        /// No price in dollars (derived from yes price).
        /// </summary>
        [JsonPropertyName("no_price_dollars")]
        public required string NoPriceDollars { get; init; }

        /// <summary>
        /// Number of contracts traded - Fixed-point (2 decimals).
        /// </summary>
        [JsonPropertyName("count_fp")]
        public required string CountFp { get; init; }

        /// <summary>
        /// Taker side of the trade.
        /// </summary>
        [JsonPropertyName("taker_side")]
        public string? TakerSide { get; init; }

        /// <summary>
        /// When this trade occurred (Unix milliseconds).
        /// </summary>
        [JsonPropertyName("ts")]
        public long? TimeStampMs { get; init; }

        /// <summary>
        /// Gets the trade creation time as a DateTimeOffset.
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset? TimeStamp => TimeStampMs.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(TimeStampMs.Value)
            : null;
    }
}
