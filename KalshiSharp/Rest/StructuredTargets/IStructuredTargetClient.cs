using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.StructuredTargets;

/// <summary>
/// Client for Kalshi structured-target endpoints.
/// </summary>
public interface IStructuredTargetClient
{
    /// <summary>Gets a structured target by ID.</summary>
    /// <param name="structuredTargetId">The structured-target ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The target, or <see langword="null"/> when a successful response omits it.</returns>
    Task<StructuredTargetResponse?> GetStructuredTargetAsync(
        string structuredTargetId,
        CancellationToken cancellationToken = default);

    /// <summary>Lists structured targets with optional filters.</summary>
    /// <param name="query">Optional target filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching targets.</returns>
    Task<StructuredTargetsResponse> ListStructuredTargetsAsync(
        StructuredTargetQuery? query = null,
        CancellationToken cancellationToken = default);
}
