using KalshiSharp.Http;
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Portfolio;

/// <summary>
/// Implementation of the portfolio client for balance, positions, and fills endpoints.
/// </summary>
internal sealed class PortfolioClient : IPortfolioClient
{
    private const string BasePath = "/trade-api/v2/portfolio";

    private readonly IKalshiHttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="PortfolioClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public PortfolioClient(IKalshiHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public Task<BalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/balance"
        };

        return _httpClient.SendAsync<BalanceResponse>(httpRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PositionsResponse> ListPositionsAsync(
        string? cursor = null,
        int? limit = null,
        string? ticker = null,
        string? eventTicker = null,
        CancellationToken cancellationToken = default)
    {
        var queryString = BuildPositionsQueryString(cursor, limit, ticker, eventTicker);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/positions{queryString}"
        };

        return _httpClient.SendAsync<PositionsResponse>(httpRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FillsResponse> ListFillsAsync(
        string? cursor = null,
        int? limit = null,
        string? ticker = null,
        string? orderId = null,
        DateTimeOffset? minTs = null,
        DateTimeOffset? maxTs = null,
        OrderSide? side = null,
        CancellationToken cancellationToken = default)
    {
        var queryString = BuildFillsQueryString(cursor, limit, ticker, orderId, minTs, maxTs, side);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/fills{queryString}"
        };

        return _httpClient.SendAsync<FillsResponse>(httpRequest, cancellationToken);
    }

    private static string BuildPositionsQueryString(
        string? cursor,
        int? limit,
        string? ticker,
        string? eventTicker)
    {
        var parameters = new List<string>();

        if (!string.IsNullOrEmpty(cursor))
        {
            parameters.Add($"cursor={Uri.EscapeDataString(cursor)}");
        }

        if (limit.HasValue)
        {
            parameters.Add($"limit={limit.Value}");
        }

        if (!string.IsNullOrEmpty(ticker))
        {
            parameters.Add($"ticker={Uri.EscapeDataString(ticker)}");
        }

        if (!string.IsNullOrEmpty(eventTicker))
        {
            parameters.Add($"event_ticker={Uri.EscapeDataString(eventTicker)}");
        }

        return parameters.Count > 0 ? $"?{string.Join("&", parameters)}" : string.Empty;
    }

    private static string BuildFillsQueryString(
        string? cursor,
        int? limit,
        string? ticker,
        string? orderId,
        DateTimeOffset? minTs,
        DateTimeOffset? maxTs,
        OrderSide? side)
    {
        var parameters = new List<string>();

        if (!string.IsNullOrEmpty(cursor))
            parameters.Add($"cursor={Uri.EscapeDataString(cursor)}");

        if (limit.HasValue)
            parameters.Add($"limit={limit.Value}");

        if (!string.IsNullOrEmpty(ticker))
            parameters.Add($"ticker={Uri.EscapeDataString(ticker)}");

        if (!string.IsNullOrEmpty(orderId))
            parameters.Add($"order_id={Uri.EscapeDataString(orderId)}");

        if (minTs.HasValue)
            parameters.Add($"min_ts={minTs.Value.ToUnixTimeSeconds()}");

        if (maxTs.HasValue)
            parameters.Add($"max_ts={maxTs.Value.ToUnixTimeSeconds()}");

        if (side.HasValue)
            parameters.Add($"side={side.Value.ToString().ToLowerInvariant()}");

        return parameters.Count > 0 ? $"?{string.Join("&", parameters)}" : string.Empty;
    }
}
