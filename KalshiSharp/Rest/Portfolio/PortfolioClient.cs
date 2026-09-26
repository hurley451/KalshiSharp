using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
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
    public Task<BalanceResponse> GetBalanceAsync(BalanceQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/balance{query.ToQueryString()}"
        };
        return _httpClient.SendAsync<BalanceResponse>(httpRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SubaccountBalancesResponse> GetSubaccountBalancesAsync(CancellationToken cancellationToken = default)
    {
        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/subaccounts/balances"
        };
        return _httpClient.SendAsync<SubaccountBalancesResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TotalRestingOrderValueResponse> GetTotalRestingOrderValueAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/summary/total_resting_order_value"
        };
        return _httpClient.SendAsync<TotalRestingOrderValueResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TargetBalanceAllocationResponse> GetTargetBalanceAllocationAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/target_balance_allocation"
        };
        return _httpClient.SendAsync<TargetBalanceAllocationResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetTargetBalanceAllocationAsync(
        SetTargetBalanceAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTargetBalanceAllocation(request);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = $"{BasePath}/target_balance_allocation",
            Content = request
        };
        return _httpClient.SendAsync(httpRequest, cancellationToken);
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
        CancellationToken cancellationToken = default)
    {
        var queryString = BuildFillsQueryString(cursor, limit, ticker, orderId);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/fills{queryString}"
        };

        return _httpClient.SendAsync<FillsResponse>(httpRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PositionsResponse> ListPositionsAsync(PositionQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/positions{query.ToQueryString()}"
        };

        return _httpClient.SendAsync<PositionsResponse>(httpRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FillsResponse> ListFillsAsync(FillQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/fills{query.ToQueryString()}"
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
        string? orderId)
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

        if (!string.IsNullOrEmpty(orderId))
        {
            parameters.Add($"order_id={Uri.EscapeDataString(orderId)}");
        }

        return parameters.Count > 0 ? $"?{string.Join("&", parameters)}" : string.Empty;
    }

    private static void ValidateTargetBalanceAllocation(SetTargetBalanceAllocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.Allocations);

        var totalPercent = 0;
        foreach (var allocation in request.Allocations)
        {
            if (allocation.ExchangeIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    "Target balance allocation exchange indexes must be nonnegative.");
            }

            if (allocation.Percent is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    "Target balance allocation percentages must be between 0 and 100.");
            }

            totalPercent += allocation.Percent;
        }

        if (request.Allocations.Count > 0 && totalPercent != 100)
        {
            throw new ArgumentException(
                "Target balance allocation percentages must total 100, or be empty to disable automatic rebalancing.",
                nameof(request));
        }
    }
}
