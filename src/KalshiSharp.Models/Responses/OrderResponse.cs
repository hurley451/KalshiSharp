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
    /// Order type (Limit or Market).
    /// </summary>
    public required OrderType Type { get; init; }

    /// <summary>
    /// Current status of the order.
    /// </summary>
    public required OrderStatus Status { get; init; }

    /// <summary>
    /// Whether this is a buy or sell action.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Total quantity ordered.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Quantity remaining (not yet filled).
    /// </summary>
    public required int RemainingCount { get; init; }

    /// <summary>
    /// Limit price in cents (1-99).
    /// </summary>
    public required int YesPrice { get; init; }

    /// <summary>
    /// No price in cents (derived from yes price).
    /// </summary>
    public required int NoPrice { get; init; }

    /// <summary>
    /// Time in force for this order.
    /// </summary>
    public required TimeInForce TimeInForce { get; init; }

    /// <summary>
    /// When the order was created.
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>
    /// When the order was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedTime { get; init; }

    /// <summary>
    /// When the order expires (for GTD orders).
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; init; }

    /// <summary>
    /// User ID that placed the order.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Reason for order cancellation or rejection.
    /// </summary>
    public string? DecreaseReason { get; init; }

    /// <summary>
    /// Whether the order should be resting only (maker).
    /// </summary>
    public bool? MakerFill { get; init; }

    /// <summary>
    /// Amount paid for fees so far (in cents).
    /// </summary>
    public int? FeesPaid { get; init; }

    /// <summary>
    /// Quantity that has been filled.
    /// </summary>
    public int FilledCount => Count - RemainingCount;
}
