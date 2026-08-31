using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.CfBenchmarks;

/// <summary>
/// Client for the authenticated CF Benchmarks REST passthrough.
/// </summary>
public interface ICfBenchmarksClient
{
    /// <summary>
    /// Gets current CF Benchmarks data from a provider-relative endpoint.
    /// </summary>
    /// <param name="relativePath">Unescaped provider path, such as <c>values</c>.</param>
    /// <param name="queryParameters">Optional provider query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CfBenchmarksResponse> GetCurrentAsync(
        string relativePath,
        IReadOnlyDictionary<string, string?>? queryParameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical CF Benchmarks data from a provider-relative endpoint.
    /// </summary>
    /// <param name="relativePath">Unescaped path below <c>history</c>, such as <c>values</c>.</param>
    /// <param name="queryParameters">Optional provider query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CfBenchmarksResponse> GetHistoricalAsync(
        string relativePath,
        IReadOnlyDictionary<string, string?>? queryParameters = null,
        CancellationToken cancellationToken = default);
}
