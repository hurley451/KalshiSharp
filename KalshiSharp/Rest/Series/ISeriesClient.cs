using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Series;

/// <summary>
/// Client for Kalshi series discovery endpoints.
/// </summary>
public interface ISeriesClient
{
    /// <summary>
    /// Gets a single series by ticker.
    /// </summary>
    /// <param name="seriesTicker">The series ticker.</param>
    /// <param name="includeVolume">Whether to include aggregate series volume.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The series details.</returns>
    Task<SeriesResponse> GetSeriesAsync(
        string seriesTicker,
        bool? includeVolume = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists series with optional filters.
    /// </summary>
    /// <param name="query">Optional series filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching series.</returns>
    Task<SeriesListResponse> ListSeriesAsync(
        SeriesQuery? query = null,
        CancellationToken cancellationToken = default);
}
