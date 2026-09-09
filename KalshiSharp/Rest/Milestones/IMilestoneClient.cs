using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Milestones;

/// <summary>
/// Client for Kalshi milestone endpoints.
/// </summary>
public interface IMilestoneClient
{
    /// <summary>Gets a milestone by ID.</summary>
    /// <param name="milestoneId">The milestone ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The milestone.</returns>
    Task<MilestoneResponse> GetMilestoneAsync(
        string milestoneId,
        CancellationToken cancellationToken = default);

    /// <summary>Lists milestones using the required page limit and optional filters.</summary>
    /// <param name="query">Milestone filters including the required limit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching milestones.</returns>
    Task<MilestonesResponse> ListMilestonesAsync(
        MilestoneQuery query,
        CancellationToken cancellationToken = default);
}
