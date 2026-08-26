using KalshiSharp.Http;
using KalshiSharp.Models.Common;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Series;

/// <summary>
/// Implementation of the series discovery client.
/// </summary>
internal sealed class SeriesClient : ISeriesClient
{
    private const string BasePath = "/trade-api/v2/series";

    private readonly IKalshiHttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public SeriesClient(IKalshiHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<SeriesResponse> GetSeriesAsync(
        string seriesTicker,
        bool? includeVolume = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seriesTicker);

        var builder = new QueryStringBuilder();
        if (includeVolume.HasValue)
        {
            builder.Append("include_volume", includeVolume.Value ? "true" : "false");
        }

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{Uri.EscapeDataString(seriesTicker)}{builder.Build()}"
        };

        var response = await _httpClient.SendAsync<SingleSeriesResponse>(request, cancellationToken).ConfigureAwait(false);
        return response.Series;
    }

    /// <inheritdoc />
    public Task<SeriesListResponse> ListSeriesAsync(
        SeriesQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}{query?.ToQueryString() ?? string.Empty}"
        };

        return _httpClient.SendAsync<SeriesListResponse>(request, cancellationToken);
    }
}
