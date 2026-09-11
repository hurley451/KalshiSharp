using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.LiveData;

/// <summary>
/// Client for Kalshi live-data endpoints.
/// </summary>
public interface ILiveDataClient
{
    /// <summary>Gets the weather index for a configured city.</summary>
    /// <param name="city">Index city ID.</param>
    /// <param name="query">Optional window and detail parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The weather-index time series.</returns>
    Task<WeatherIndexResponse> GetWeatherIndexAsync(
        string city,
        WeatherIndexQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the weather-index calibration timeline for a configured city.</summary>
    /// <param name="city">Index city ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The calibration timeline.</returns>
    Task<WeatherIndexCalibrationsResponse> GetWeatherIndexCalibrationsAsync(
        string city,
        CancellationToken cancellationToken = default);
}
