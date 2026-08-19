using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Historical;

/// <summary>HTTP implementation of the historical exchange-data client.</summary>
internal sealed class HistoricalClient : IHistoricalClient
{
    private const string BasePath = "/trade-api/v2/historical";
    private readonly IKalshiHttpClient _httpClient;

    /// <summary>Initializes the historical client.</summary>
    public HistoricalClient(IKalshiHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public Task<HistoricalCutoffResponse> GetCutoffAsync(CancellationToken cancellationToken = default) =>
        GetAsync<HistoricalCutoffResponse>("/cutoff", cancellationToken);

    /// <inheritdoc />
    public Task<MarketsResponse> ListMarketsAsync(HistoricalMarketQuery? query = null, CancellationToken cancellationToken = default) =>
        GetAsync<MarketsResponse>($"/markets{query?.ToQueryString()}", cancellationToken);

    /// <inheritdoc />
    public async Task<MarketResponse> GetMarketAsync(string ticker, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
        var response = await GetAsync<SingleMarketResponse>($"/markets/{Uri.EscapeDataString(ticker)}", cancellationToken);
        return response.Market;
    }

    /// <inheritdoc />
    public Task<HistoricalMarketCandlesticksResponse> GetMarketCandlesticksAsync(
        string ticker,
        MarketCandlesticksQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
        ArgumentNullException.ThrowIfNull(query);
        return GetAsync<HistoricalMarketCandlesticksResponse>(
            $"/markets/{Uri.EscapeDataString(ticker)}/candlesticks{query.ToQueryString()}",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TradesResponse> ListTradesAsync(HistoricalTradeQuery? query = null, CancellationToken cancellationToken = default) =>
        GetAsync<TradesResponse>($"/trades{query?.ToQueryString()}", cancellationToken);

    /// <inheritdoc />
    public Task<FillsResponse> ListFillsAsync(HistoricalFillQuery? query = null, CancellationToken cancellationToken = default) =>
        GetAsync<FillsResponse>($"/fills{query?.ToQueryString()}", cancellationToken);

    /// <inheritdoc />
    public Task<OrdersResponse> ListOrdersAsync(HistoricalOrderQuery? query = null, CancellationToken cancellationToken = default) =>
        GetAsync<OrdersResponse>($"/orders{query?.ToQueryString()}", cancellationToken);

    /// <inheritdoc />
    public Task<PositionsResponse> ListPositionsAsync(HistoricalPositionQuery? query = null, CancellationToken cancellationToken = default) =>
        GetAsync<PositionsResponse>($"/positions{query?.ToQueryString()}", cancellationToken);

    private Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
        where T : class
    {
        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = BasePath + path
        };
        return _httpClient.SendAsync<T>(request, cancellationToken);
    }
}
