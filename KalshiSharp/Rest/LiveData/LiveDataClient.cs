using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.LiveData;

internal sealed class LiveDataClient(IKalshiHttpClient httpClient) : ILiveDataClient
{
    private const string BasePath = "/trade-api/v2/live_data";
    private readonly IKalshiHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

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
