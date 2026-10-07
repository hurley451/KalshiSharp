using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Communications;

/// <summary>
/// Client for Kalshi communications endpoints.
/// </summary>
public interface ICommunicationsClient
{
    /// <summary>Gets the caller's public communications ID.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The communications ID response.</returns>
    Task<CommunicationsIdResponse> GetCommunicationsIdAsync(CancellationToken cancellationToken = default);
}
