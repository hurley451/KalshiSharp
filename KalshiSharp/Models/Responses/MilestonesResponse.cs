using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response returned by the List Milestones endpoint.
/// </summary>
public sealed record MilestonesResponse : PagedResponse<MilestoneResponse>
{
    /// <summary>The milestones in this page.</summary>
    public required IReadOnlyList<MilestoneResponse> Milestones { get; init; }

    /// <inheritdoc />
    public override IReadOnlyList<MilestoneResponse> Items => Milestones;
}
