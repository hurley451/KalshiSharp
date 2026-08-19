using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response for listing positions.
/// </summary>
public sealed record PositionsResponse : PagedResponse<PositionResponse>
{
    /// <summary>
    /// The positions in this page.
    /// </summary>
    public IReadOnlyList<PositionResponse> Positions { get; init; } = [];

    /// <summary>Market positions under the current response contract.</summary>
    public IReadOnlyList<PositionResponse> MarketPositions { get; init; } = [];

    /// <summary>Aggregate event positions.</summary>
    public IReadOnlyList<EventPositionResponse> EventPositions { get; init; } = [];

    /// <inheritdoc />
    public override IReadOnlyList<PositionResponse> Items => MarketPositions.Count > 0 ? MarketPositions : Positions;
}

/// <summary>Aggregate position across an event.</summary>
public sealed record EventPositionResponse
{
    /// <summary>Event ticker.</summary>
    public required string EventTicker { get; init; }

    /// <summary>Total cost in dollars.</summary>
    public string? TotalCostDollars { get; init; }

    /// <summary>Total cost shares as a fixed-point count.</summary>
    public string? TotalCostSharesFp { get; init; }

    /// <summary>Event exposure in dollars.</summary>
    public string? EventExposureDollars { get; init; }

    /// <summary>Realized profit or loss in dollars.</summary>
    public string? RealizedPnlDollars { get; init; }

    /// <summary>Fees paid in dollars.</summary>
    public string? FeesPaidDollars { get; init; }
}
