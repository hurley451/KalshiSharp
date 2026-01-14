using System.Globalization;
using KalshiSharp.Http;
using KalshiSharp.Models.Common;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Markets;

/// <summary>
/// Implementation of the market client for market-related endpoints.
/// </summary>
internal sealed class MarketClient : IMarketClient
{
    private const string BasePath = "/trade-api/v2/markets";

    private readonly IKalshiHttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public MarketClient(IKalshiHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<MarketResponse> GetMarketAsync(string ticker, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{Uri.EscapeDataString(ticker)}"
        };

        var response = await _httpClient.SendAsync<SingleMarketResponse>(request, cancellationToken);
        return response.Market;
    }

    /// <inheritdoc />
    public Task<MarketsResponse> ListMarketsAsync(MarketQuery? query = null, CancellationToken cancellationToken = default)
    {
        var queryString = query?.ToQueryString() ?? string.Empty;

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}{queryString}"
        };

        return _httpClient.SendAsync<MarketsResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OrderBookResponse> GetOrderBookAsync(string ticker, int? depth = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);

        var builder = new QueryStringBuilder();
        if (depth.HasValue)
        {
            builder.Append("depth", depth.Value.ToString(CultureInfo.InvariantCulture));
        }

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{Uri.EscapeDataString(ticker)}/orderbook{builder.Build()}"
        };

        return _httpClient.SendAsync<OrderBookResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TradesResponse> GetTradesAsync(string ticker, string? cursor = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);

        var builder = new QueryStringBuilder();
        builder.AppendIfNotEmpty("cursor", cursor);
        if (limit.HasValue)
        {
            builder.Append("limit", limit.Value.ToString(CultureInfo.InvariantCulture));
        }

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{Uri.EscapeDataString(ticker)}/trades{builder.Build()}"
        };

        return _httpClient.SendAsync<TradesResponse>(request, cancellationToken);
    }
}
