using System;
using System.Text.Json.Serialization;

namespace KalshiSharp.Models.WebSocket
{
    /// <summary>
    /// Real-time position updates for authenticated user.
    /// </summary>
    public sealed record MarketPositionUpdate : WebSocketMessage<MarketPositionUpdate.MessageBody>
    {
        /// <inheritdoc/>
        public override string Type => "market_position";

        /// <summary>
        /// Message body containing position update details.
        /// </summary>
        public sealed record MessageBody
        {
            /// <summary>
            /// User ID for the position.
            /// </summary>
            [JsonPropertyName("user_id")]
            public required string UserId { get; init; }

            /// <summary>
            /// Unique market identifier.
            /// </summary>
            [JsonPropertyName("market_ticker")]
            public required string MarketTicker { get; init; }

            /// <summary>
            /// Current net position (positive for long, negative for short).
            /// </summary>
            [JsonPropertyName("position")]
            public int Position { get; init; }

            /// <summary>
            /// Fixed-point net position (2 decimals).
            /// </summary>
            [JsonPropertyName("position_fp")]
            public string? PositionFp { get; init; }

            /// <summary>
            /// Current cost basis of the position in centi-cents (1/10,000th of a dollar).
            /// </summary>
            [JsonPropertyName("position_cost")]
            public int PositionCost { get; init; }

            /// <summary>
            /// Realized profit/loss in centi-cents (1/10,000th of a dollar).
            /// </summary>
            [JsonPropertyName("realized_pnl")]
            public int RealizedPnl { get; init; }

            /// <summary>
            /// Total fees paid in centi-cents (1/10,000th of a dollar).
            /// </summary>
            [JsonPropertyName("fees_paid")]
            public int FeesPaid { get; init; }

            /// <summary>
            /// Total position fee cost in centi-cents (1/10,000th of a dollar).
            /// </summary>
            [JsonPropertyName("position_fee_cost")]
            public int PositionFeeCost { get; init; }

            /// <summary>
            /// Total volume traded.
            /// </summary>
            [JsonPropertyName("volume")]
            public int Volume { get; init; }

            /// <summary>
            /// Fixed-point total volume traded (2 decimals).
            /// </summary>
            [JsonPropertyName("volume_fp")]
            public string? VolumeFp { get; init; }

            /// <summary>
            /// Optional subaccount number for the position.
            /// </summary>
            [JsonPropertyName("subaccount")]
            public int Subaccount { get; init; }
        }
    }
}
