using System.Text.Json.Serialization;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.WebSocket;

/// <summary>Private fill update for the authenticated user.</summary>
public sealed record FillUpdate : WebSocketMessage<FillUpdate.MessageBody>
{
        /// <inheritdoc/>
        public override string Type => "fill";

        /// <summary>
        /// Message body containing fill details.
        /// </summary>
        public sealed record MessageBody
        {
            /// <summary>
            /// Unique identifier for fills. This is what you use to differentiate fills.
            /// </summary>
            [JsonPropertyName("trade_id")]
            public required string TradeId { get; init; }

            /// <summary>
            /// Unique identifier for orders. This is what you use to differentiate fills for different orders.
            /// </summary>
            [JsonPropertyName("order_id")]
            public required string OrderId { get; init; }

            /// <summary>
            /// Unique market identifier.
            /// </summary>
            [JsonPropertyName("market_ticker")]
            public required string MarketTicker { get; init; }

            /// <summary>
            /// Exchange shard where the fill occurred.
            /// </summary>
            [JsonPropertyName("exchange_index")]
            public int? ExchangeIndex { get; init; }

            /// <summary>
            /// If you were a taker on this fill.
            /// </summary>
            [JsonPropertyName("is_taker")]
            public required bool IsTaker { get; init; }

            /// <summary>
            /// Market side.
            /// </summary>
            [JsonPropertyName("side")]
            public OrderSide? Side { get; init; }

            /// <summary>
            /// Price for the yes side of the fill in dollars.
            /// </summary>
            [JsonPropertyName("yes_price_dollars")]
            public string? YesPriceDollars { get; init; }

            /// <summary>NO execution price in dollars.</summary>
            public string? NoPriceDollars { get; init; }

            /// <summary>
            /// Fixed-point contracts filled (2 decimals).
            /// </summary>
            [JsonPropertyName("count_fp")]
            public string? CountFp { get; init; }

            /// <summary>
            /// Fixed-point position after the fill (2 decimals).
            /// </summary>
            [JsonPropertyName("post_position_fp")]
            public string? PostPositionFp { get; init; }

            /// <summary>
            /// Exchange fee paid for this fill in fixed-point dollars.
            /// </summary>
            [JsonPropertyName("fee_cost")]
            public string? FeeCost { get; init; }

            /// <summary>
            /// Order action type.
            /// </summary>
            [JsonPropertyName("action")]
            public string? Action { get; init; }

            /// <summary>
            /// Unix timestamp for when the update happened (in seconds).
            /// </summary>
            [JsonPropertyName("ts")]
            public long? Ts { get; init; }

            /// <summary>Unix timestamp in milliseconds.</summary>
            [JsonPropertyName("ts_ms")]
            public long? TsMs { get; init; }

            /// <summary>
            /// Optional client-provided order ID.
            /// </summary>
            [JsonPropertyName("client_order_id")]
            public string? ClientOrderId { get; init; }

            /// <summary>
            /// Market side.
            /// </summary>
            [JsonPropertyName("purchased_side")]
            public OrderSide? PurchasedSide { get; init; }

            /// <summary>Canonical outcome side, when supplied.</summary>
            public OrderSide? OutcomeSide { get; init; }

            /// <summary>Canonical book side, when supplied.</summary>
            public OrderBookSide? BookSide { get; init; }

            /// <summary>
            /// Optional subaccount number for the fill.
            /// </summary>
            [JsonPropertyName("subaccount")]
            public int? Subaccount { get; init; }
        }
}
