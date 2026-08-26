using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Incentives;

/// <summary>
/// Client for Kalshi incentive-program endpoints.
/// </summary>
public interface IIncentiveClient
{
    /// <summary>Lists incentive programs with optional filtering and pagination.</summary>
    /// <param name="query">Optional filters and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A page of incentive programs.</returns>
    Task<IncentiveProgramsResponse> ListIncentiveProgramsAsync(
        IncentiveProgramQuery? query = null,
        CancellationToken cancellationToken = default);
}
