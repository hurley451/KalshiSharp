using System.Text.Json.Serialization;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.WebSocket
{
    /// <summary>
    /// Private fill information for authenticated user.
    /// </summary>
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
            /// If you were a taker on this fill.
            /// </summary>
            [JsonPropertyName("is_taker")]
            public required bool IsTaker { get; init; }

            /// <summary>
            /// Market side.
            /// </summary>
            [JsonPropertyName("side")]
            public required OrderSide Side { get; init; }

            /// <summary>
            /// Price for the yes side of the fill. Between 1 and 99 (inclusive).
            /// </summary>
            [JsonPropertyName("yes_price")]
            public int YesPrice { get; init; }

            /// <summary>
            /// Price for the yes side of the fill in dollars.
            /// </summary>
            [JsonPropertyName("yes_price_dollars")]
            public required string YesPriceDollars { get; init; }

            /// <summary>
            /// Number of contracts filled.
            /// </summary>
            [JsonPropertyName("count")]
            public required int Count { get; init; }

            /// <summary>
            /// Fixed-point contracts filled (2 decimals).
            /// </summary>
            [JsonPropertyName("count_fp")]
            public required string CountFp { get; init; }

            /// <summary>
            /// Exchange fee paid for this fill in fixed-point dollars.
            /// </summary>
            [JsonPropertyName("fee_cost")]
            public required string FeeCost { get; init; }

            /// <summary>
            /// Order action type.
            /// </summary>
            [JsonPropertyName("action")]
            public required string Action { get; init; }

            /// <summary>
            /// Unix timestamp for when the update happened (in seconds).
            /// </summary>
            [JsonPropertyName("ts")]
            public required long Ts { get; init; }

            /// <summary>
            /// Optional client-provided order ID.
            /// </summary>
            [JsonPropertyName("client_order_id")]
            public string? ClientOrderId { get; init; }

            /// <summary>
            /// Position after the fill.
            /// </summary>
            [JsonPropertyName("post_position")]
            public required int PostPosition { get; init; }

            /// <summary>
            /// Fixed-point position after the fill (2 decimals).
            /// </summary>
            [JsonPropertyName("post_position_fp")]
            public required string PostPositionFp { get; init; }

            /// <summary>
            /// Market side.
            /// </summary>
            [JsonPropertyName("purchased_side")]
            public required OrderSide PurchasedSide { get; init; }

            /// <summary>
            /// Optional subaccount number for the fill.
            /// </summary>
            [JsonPropertyName("subaccount")]
            public int? Subaccount { get; init; }

            /// <summary>
            /// Calculated No price (derived from YesPrice).
            /// </summary>
            [JsonIgnore]
            public int NoPrice
            {
                get
                {
                    const int FullPrice = 100;
                    return FullPrice - YesPrice;
                }
            }                    

            /// <summary>
            /// Price filled at.
            /// </summary>
            /// <remarks>
            /// If the order side is No then the FillPrice is equivalent to the NoPrice.
            /// If the order side is Yes, then the FillPrice is equivalent to the YesPrice.
            /// </remarks>
            [JsonIgnore]
            public int FillPrice => Side == OrderSide.Yes ? YesPrice : NoPrice;
        }
    }
}
