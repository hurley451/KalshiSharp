using KalshiSharp.Http;
using KalshiSharp.Models.Common;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using System.Globalization;

namespace KalshiSharp.Rest.Markets;

/// <summary>
/// Implementation of the market client for market-related endpoints.
/// </summary>
internal sealed class MarketClient : IMarketClient
{
    private const string BasePath = "/trade-api/v2/markets";
    private const string SeriesBasePath = "/trade-api/v2/series";

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
    public Task<MultipleOrderBooksResponse> GetOrderBooksAsync(MultipleOrderBooksQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/orderbooks{query.ToQueryString()}"
        };
        return _httpClient.SendAsync<MultipleOrderBooksResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TradesResponse> GetTradesAsync(string ticker, string? cursor = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);

        var builder = new QueryStringBuilder();
        builder.Append("ticker", ticker);
        builder.AppendIfNotEmpty("cursor", cursor);
        if (limit.HasValue)
        {
            builder.Append("limit", limit.Value.ToString(CultureInfo.InvariantCulture));
        }

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/trades{builder.Build()}"
        };

        return _httpClient.SendAsync<TradesResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TradesResponse> GetTradesAsync(MarketTradeQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/trades{query.ToQueryString()}"
        };
        return _httpClient.SendAsync<TradesResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MarketCandlesticksResponse> GetMarketCandlesticksAsync(string seriesTicker, string ticker, MarketCandlesticksQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seriesTicker);
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
        ArgumentNullException.ThrowIfNull(query);

        var queryString = query.ToQueryString();

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{SeriesBasePath}/{Uri.EscapeDataString(seriesTicker)}/markets/{Uri.EscapeDataString(ticker)}/candlesticks{queryString}"
        };

        return _httpClient.SendAsync<MarketCandlesticksResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<BatchMarketCandlesticksResponse> GetBatchMarketCandlesticksAsync(BatchMarketCandlesticksQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.MarketTickers is null || query.MarketTickers.Count == 0)
            throw new ArgumentException("At least one market ticker is required.", nameof(query));

        if (query.MarketTickers.Count > 100)
            throw new ArgumentException("A maximum of 100 market tickers is allowed per request.", nameof(query));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/candlesticks{query.ToQueryString()}"
        };

        return _httpClient.SendAsync<BatchMarketCandlesticksResponse>(request, cancellationToken);
    }
}
