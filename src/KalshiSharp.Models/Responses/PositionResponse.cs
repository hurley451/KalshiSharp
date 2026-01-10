namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents a position the user holds in a market.
/// </summary>
public sealed record PositionResponse
{
    /// <summary>
    /// Market ticker for this position.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Event ticker for the market.
    /// </summary>
    public required string EventTicker { get; init; }

    /// <summary>
    /// Market exposure - positive means long Yes, negative means long No.
    /// </summary>
    public required int MarketExposure { get; init; }

    /// <summary>
    /// Total position in cents (absolute value).
    /// </summary>
    public required int Position { get; init; }

    /// <summary>
    /// Number of Yes contracts held.
    /// </summary>
    public required int YesContracts { get; init; }

    /// <summary>
    /// Number of No contracts held.
    /// </summary>
    public required int NoContracts { get; init; }

    /// <summary>
    /// Average price paid for the position.
    /// </summary>
    public int? AveragePricePaid { get; init; }

    /// <summary>
    /// Total amount spent acquiring this position in cents.
    /// </summary>
    public int? TotalCost { get; init; }

    /// <summary>
    /// Realized profit/loss in cents.
    /// </summary>
    public int? RealizedPnl { get; init; }

    /// <summary>
    /// Resting orders count for this market.
    /// </summary>
    public int? RestingOrdersCount { get; init; }

    /// <summary>
    /// Fee exposure for this position.
    /// </summary>
    public int? FeesPaid { get; init; }
}
