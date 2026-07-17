using KalshiSharp.Models.Enums;
using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents an order on the Kalshi exchange.
/// </summary>
public sealed record OrderResponse
{
    /// <summary>
    /// Unique identifier for this order.
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// User ID that placed the order.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Client-provided order identifier for correlation.
    /// </summary>
    public string? ClientOrderId { get; init; }

    /// <summary>
    /// Market ticker this order is for.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Order side (Yes or No).
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// Whether this is a buy or sell action.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Order type (Limit or Market).
    /// </summary>
    public required OrderType Type { get; init; }

    /// <summary>
    /// Current status of the order.
    /// </summary>
    public required OrderStatus Status { get; init; }

    /// <summary>
    /// Yes price in dollars (string representation).
    /// </summary>
    [JsonPropertyName("yes_price_dollars")]
    public string? YesPriceDollars { get; init; }

    /// <summary>
    /// No price in dollars (string representation).
    /// </summary>
    [JsonPropertyName("no_price_dollars")]
    public string? NoPriceDollars { get; init; }

    /// <summary>
    /// Number of contracts filled - Fixed-point (2 decimals).
    /// </summary>
    [JsonPropertyName("fill_count_fp")]
    public string? FillCountFp { get; init; }

    /// <summary>
    /// Quantity remaining (not yet filled) - Fixed-point (2 decimals).
    /// </summary>
    [JsonPropertyName("remaining_count_fp")]
    public string? RemainingCountFp { get; init; }

    /// <summary>
    /// Initial quantity ordered - Fixed-point (2 decimals).
    /// </summary>
    [JsonPropertyName("initial_count_fp")]
    public string? InitialCountFp { get; init; }

    /// <summary>
    /// Taker fill cost in dollars (string representation).
    /// </summary>
    [JsonPropertyName("taker_fill_cost_dollars")]
    public string? TakerFillCostDollars { get; init; }

    /// <summary>
    /// Maker fill cost in dollars (string representation).
    /// </summary>
    [JsonPropertyName("maker_fill_cost_dollars")]
    public string? MakerFillCostDollars { get; init; }

    /// <summary>
    /// Taker fees in dollars (string representation).
    /// </summary>
    [JsonPropertyName("taker_fees_dollars")]
    public string? TakerFeesDollars { get; init; }

    /// <summary>
    /// Maker fees in dollars (string representation).
    /// </summary>
    [JsonPropertyName("maker_fees_dollars")]
    public string? MakerFeesDollars { get; init; }

    /// <summary>
    /// Queue position for resting orders.
    /// </summary>
    public int? QueuePosition { get; init; }

    /// <summary>
    /// When the order expires.
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; init; }

    /// <summary>
    /// When the order was created.
    /// </summary>
    public DateTimeOffset? CreatedTime { get; init; }

    /// <summary>
    /// When the order was last updated.
    /// </summary>
    public DateTimeOffset? LastUpdateTime { get; init; }

    /// <summary>
    /// Self-trade prevention type.
    /// </summary>
    public string? SelfTradePreventionType { get; init; }

    /// <summary>
    /// Order group ID for batch orders.
    /// </summary>
    public string? OrderGroupId { get; init; }

    /// <summary>
    /// Whether to cancel order when market is paused.
    /// </summary>
    public bool? CancelOrderOnPause { get; init; }

    /// <summary>
    /// Directional exposure side of the order.
    /// buy-yes and sell-no ? yes; buy-no and sell-yes ? no.
    /// Prefer this over <see cref="Side"/> when present.
    /// Will replace the legacy <see cref="Side"/> field in a future API release.
    /// </summary>
    [JsonPropertyName("outcome_side")]
    public OrderSide? OutcomeSide { get; init; }

    /// <summary>
    /// Book-vocabulary equivalent of <see cref="OutcomeSide"/>.
    /// bid = outcome yes; ask = outcome no.
    /// Will replace the legacy <see cref="Side"/> field in a future API release.
    /// </summary>
    [JsonPropertyName("book_side")]
    public OrderBookSide? BookSide { get; init; }
}
