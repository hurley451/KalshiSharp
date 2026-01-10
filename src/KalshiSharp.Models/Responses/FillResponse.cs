using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents a fill (partial or complete execution of an order).
/// </summary>
public sealed record FillResponse
{
    /// <summary>
    /// Unique identifier for this fill.
    /// </summary>
    public required string TradeId { get; init; }

    /// <summary>
    /// Order ID this fill belongs to.
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// Market ticker for this fill.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Side of the order (Yes or No).
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// Action (buy or sell).
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Number of contracts filled.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Yes price at which the fill executed (in cents).
    /// </summary>
    public required int YesPrice { get; init; }

    /// <summary>
    /// No price at which the fill executed (in cents).
    /// </summary>
    public required int NoPrice { get; init; }

    /// <summary>
    /// Whether this was a maker or taker fill.
    /// </summary>
    public required bool IsTaker { get; init; }

    /// <summary>
    /// When this fill occurred.
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }
}
