using KalshiSharp.Models.Enums;

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
    /// Yes price in cents (1-99).
    /// </summary>
    public required int YesPrice { get; init; }

    /// <summary>
    /// No price in cents (derived from yes price).
    /// </summary>
    public required int NoPrice { get; init; }

    /// <summary>
    /// Yes price in dollars (string representation).
    /// </summary>
    public string? YesPriceDollars { get; init; }

    /// <summary>
    /// No price in dollars (string representation).
    /// </summary>
    public string? NoPriceDollars { get; init; }

    /// <summary>
    /// Number of contracts filled.
    /// </summary>
    public required int FillCount { get; init; }

    /// <summary>
    /// Quantity remaining (not yet filled).
    /// </summary>
    public required int RemainingCount { get; init; }

    /// <summary>
    /// Initial quantity ordered.
    /// </summary>
    public required int InitialCount { get; init; }

    /// <summary>
    /// Taker fees in cents.
    /// </summary>
    public int? TakerFees { get; init; }

    /// <summary>
    /// Maker fees in cents.
    /// </summary>
    public int? MakerFees { get; init; }

    /// <summary>
    /// Taker fill cost in cents.
    /// </summary>
    public int? TakerFillCost { get; init; }

    /// <summary>
    /// Maker fill cost in cents.
    /// </summary>
    public int? MakerFillCost { get; init; }

    /// <summary>
    /// Taker fill cost in dollars (string representation).
    /// </summary>
    public string? TakerFillCostDollars { get; init; }

    /// <summary>
    /// Maker fill cost in dollars (string representation).
    /// </summary>
    public string? MakerFillCostDollars { get; init; }

    /// <summary>
    /// Queue position for resting orders.
    /// </summary>
    public int? QueuePosition { get; init; }

    /// <summary>
    /// Taker fees in dollars (string representation).
    /// </summary>
    public string? TakerFeesDollars { get; init; }

    /// <summary>
    /// Maker fees in dollars (string representation).
    /// </summary>
    public string? MakerFeesDollars { get; init; }

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
}
