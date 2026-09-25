using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Target balance allocations across exchange indexes.
/// </summary>
public sealed record TargetBalanceAllocationResponse
{
    /// <summary>Current target allocations.</summary>
    public IReadOnlyList<TargetBalanceAllocationEntry> Allocations { get; init; } = [];

    /// <summary>Current collateral reservation policy for resting orders, when returned.</summary>
    public RestingMarginReservation? RestingMarginReservation { get; init; }
}

/// <summary>
/// Target balance percentage for one exchange index.
/// </summary>
public sealed record TargetBalanceAllocationEntry
{
    /// <summary>Exchange shard index.</summary>
    public required int ExchangeIndex { get; init; }

    /// <summary>Integer allocation percentage.</summary>
    public required int Percent { get; init; }
}
