using System.Text.Json.Serialization;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.WebSocket;

/// <summary>
/// Real-time order update for authenticated user from the WebSocket stream.
/// Sent when an order is created, filled, partially filled, or cancelled.
/// </summary>
public sealed record OrderUpdate : WebSocketMessage<OrderUpdate.MessageBody>
{
    /// <inheritdoc/>
    public override string Type => "user_order";

    /// <summary>
    /// Message body containing order update details.
    /// </summary>
    public sealed record MessageBody
    {
        /// <summary>
        /// Unique order identifier.
        /// </summary>
        [JsonPropertyName("order_id")]
        public required string OrderId { get; init; }

        /// <summary>
        /// User identifier.
        /// </summary>
        [JsonPropertyName("user_id")]
        public required string UserId { get; init; }

        /// <summary>
        /// Unique market identifier.
        /// </summary>
        [JsonPropertyName("ticker")]
        public required string Ticker { get; init; }

        /// <summary>
        /// Current order status.
        /// </summary>
        [JsonPropertyName("status")]
        public required OrderStatus Status { get; init; }

        /// <summary>
        /// Market side.
        /// </summary>
        [JsonPropertyName("side")]
        public required OrderSide Side { get; init; }

        /// <summary>
        /// Yes price in fixed-point dollars (4 decimals).
        /// </summary>
        [JsonPropertyName("yes_price_dollars")]
        public required string YesPriceDollars { get; init; }

        /// <summary>
        /// Number of contracts filled in fixed-point (2 decimals).
        /// </summary>
        [JsonPropertyName("fill_count_fp")]
        public required string FillCountFp { get; init; }

        /// <summary>
        /// Number of contracts remaining in fixed-point (2 decimals).
        /// </summary>
        [JsonPropertyName("remaining_count_fp")]
        public required string RemainingCountFp { get; init; }

        /// <summary>
        /// Initial number of contracts in fixed-point (2 decimals).
        /// </summary>
        [JsonPropertyName("initial_count_fp")]
        public required string InitialCountFp { get; init; }

        /// <summary>
        /// Taker fill cost in fixed-point dollars (4 decimals).
        /// </summary>
        [JsonPropertyName("taker_fill_cost_dollars")]
        public string? TakerFillCostDollars { get; init; }

        /// <summary>
        /// Maker fill cost in fixed-point dollars (4 decimals).
        /// </summary>
        [JsonPropertyName("maker_fill_cost_dollars")]
        public string? MakerFillCostDollars { get; init; }

        /// <summary>
        /// Taker fees in fixed-point dollars (4 decimals). Omitted when zero.
        /// </summary>
        [JsonPropertyName("taker_fees_dollars")]
        public string? TakerFeesDollars { get; init; }

        /// <summary>
        /// Maker fees in fixed-point dollars (4 decimals). Omitted when zero.
        /// </summary>
        [JsonPropertyName("maker_fees_dollars")]
        public string? MakerFeesDollars { get; init; }

        /// <summary>
        /// Client-provided order identifier.
        /// </summary>
        [JsonPropertyName("client_order_id")]
        public string? ClientOrderId { get; init; }
      
        /// <summary>
        /// Order group identifier, if applicable.
        /// </summary>
        [JsonPropertyName("order_group_id")]
        public string? OrderGroupId { get; init; }

        /// <summary>
        /// Self-trade prevention type.
        /// </summary>
        [JsonPropertyName("self_trade_prevention_type")]
        public string? SelfTradePreventionType { get; init; }

        /// <summary>
        /// Order creation time.
        /// </summary>
        [JsonPropertyName("created_time")]
        public DateTimeOffset CreatedTime { get; init; }

        /// <summary>
        /// Last update time.
        /// </summary>
        [JsonPropertyName("last_update_time")]
        public DateTimeOffset LastUpdateTime { get; init; }

        /// <summary>
        /// Order expiration time.
        /// </summary>
        [JsonPropertyName("expiration_time")]
        public DateTimeOffset? ExpirationTime { get; init; }

        /// <summary>
        /// Subaccount number (0 for primary, 1-32 for subaccounts).
        /// </summary>
        [JsonPropertyName("subaccount_number")]
        public int? SubaccountNumber { get; init; }

        /// <summary>
        /// Calculated NoPriceInDollars
        /// </summary>
        [JsonIgnore]
        public string NoPriceInDollars
        {
            get
            {
                if (decimal.TryParse(YesPriceDollars, out decimal yesPrice))
                {
                    const decimal FullPrice = 1.0m;
                    var noPrice = FullPrice - yesPrice;
                    return noPrice.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
                }
                return "0.0000";
            }
        }

        [JsonIgnore]
        public string OrderPrice => Side == OrderSide.Yes ? YesPriceDollars : NoPriceInDollars;
    }
}
