using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.LiveData;

internal sealed class LiveDataClient(IKalshiHttpClient httpClient) : ILiveDataClient
{
    private const string BasePath = "/trade-api/v2/live_data";
    private readonly IKalshiHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <inheritdoc />
    public Task<LiveDataResponse> GetLiveDataAsync(
        string milestoneId,
        LiveDataQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(milestoneId);

        return _httpClient.SendAsync<LiveDataResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/milestone/{Uri.EscapeDataString(milestoneId)}{query?.ToQueryString()}"
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<LiveDataResponse> GetLiveDataAsync(
        string type,
        string milestoneId,
        LiveDataQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(milestoneId);

        return _httpClient.SendAsync<LiveDataResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{Uri.EscapeDataString(type)}/milestone/{Uri.EscapeDataString(milestoneId)}{query?.ToQueryString()}"
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<LiveDataBatchResponse> GetLiveDataBatchAsync(CancellationToken cancellationToken = default)
    {
        return _httpClient.SendAsync<LiveDataBatchResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/batch"
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<EventLiveDataResponse> GetEventLiveDataAsync(
        string eventTicker,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventTicker);

        return _httpClient.SendAsync<EventLiveDataResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/events/{Uri.EscapeDataString(eventTicker)}"
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<GameStatsResponse> GetGameStatsAsync(
        string milestoneId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(milestoneId);

        return _httpClient.SendAsync<GameStatsResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/milestone/{Uri.EscapeDataString(milestoneId)}/game_stats"
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<WeatherIndexResponse> GetWeatherIndexAsync(
        string city,
        WeatherIndexQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        return _httpClient.SendAsync<WeatherIndexResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/weather/{Uri.EscapeDataString(city)}{query?.ToQueryString()}"
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<WeatherIndexCalibrationsResponse> GetWeatherIndexCalibrationsAsync(
        string city,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        return _httpClient.SendAsync<WeatherIndexCalibrationsResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/weather/{Uri.EscapeDataString(city)}/calibrations"
        }, cancellationToken);
    }
}
