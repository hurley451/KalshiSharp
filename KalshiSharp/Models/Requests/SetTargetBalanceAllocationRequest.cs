using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Request to replace target balance allocations across exchange indexes.
/// </summary>
public sealed record SetTargetBalanceAllocationRequest
{
    /// <summary>
    /// Target allocations. Percentages must total 100 when non-empty; an empty list disables automatic rebalancing.
    /// </summary>
    public required IReadOnlyList<TargetBalanceAllocationRequest> Allocations { get; init; }

    /// <summary>
    /// Optional collateral reservation policy for resting orders. Omit to use Kalshi's default of <see cref="RestingMarginReservation.Sum"/>.
    /// </summary>
    public RestingMarginReservation? RestingMarginReservation { get; init; }
}

/// <summary>
/// Target balance percentage for one exchange index.
/// </summary>
public sealed record TargetBalanceAllocationRequest
{
    /// <summary>Exchange shard index.</summary>
    public required int ExchangeIndex { get; init; }

    /// <summary>Integer allocation percentage.</summary>
    public required int Percent { get; init; }
}
