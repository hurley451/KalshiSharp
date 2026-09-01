namespace KalshiSharp.Models.Responses;

/// <summary>
/// Wrapper returned by the Get Milestone endpoint.
/// </summary>
public sealed record SingleMilestoneResponse
{
    /// <summary>The milestone.</summary>
    public required MilestoneResponse Milestone { get; init; }
}
