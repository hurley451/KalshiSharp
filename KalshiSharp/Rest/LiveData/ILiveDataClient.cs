using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.LiveData;

/// <summary>
/// Client for Kalshi live-data endpoints.
/// </summary>
public interface ILiveDataClient
{
    /// <summary>Gets current live data for a milestone.</summary>
    /// <param name="milestoneId">Milestone ID.</param>
    /// <param name="query">Optional live-data query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The live-data payload.</returns>
    Task<LiveDataResponse> GetLiveDataAsync(
        string milestoneId,
        LiveDataQuery? query = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This live-data client does not support generic milestone live data.");

    /// <summary>Gets legacy type-scoped live data for a milestone.</summary>
    /// <param name="type">Legacy live-data type.</param>
    /// <param name="milestoneId">Milestone ID.</param>
    /// <param name="query">Optional live-data query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The live-data payload.</returns>
    Task<LiveDataResponse> GetLiveDataAsync(
        string type,
        string milestoneId,
        LiveDataQuery? query = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This live-data client does not support legacy typed milestone live data.");

    /// <summary>Gets live data for multiple milestones.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Available live-data payloads.</returns>
    Task<LiveDataBatchResponse> GetLiveDataBatchAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This live-data client does not support batch live data.");

    /// <summary>Gets event-keyed live data.</summary>
    /// <param name="eventTicker">Event ticker.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event live-data payload.</returns>
    Task<EventLiveDataResponse> GetEventLiveDataAsync(
        string eventTicker,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This live-data client does not support event live data.");

    /// <summary>Gets play-by-play game statistics for a milestone.</summary>
    /// <param name="milestoneId">Milestone ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The game-stats payload.</returns>
    Task<GameStatsResponse> GetGameStatsAsync(
        string milestoneId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This live-data client does not support game stats.");

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
