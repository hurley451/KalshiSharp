using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Historical;

/// <summary>Client for explicitly querying archived exchange data.</summary>
public interface IHistoricalClient
{
    /// <summary>Gets the live-to-historical cutoff timestamps.</summary>
    Task<HistoricalCutoffResponse> GetCutoffAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists archived markets.</summary>
    Task<MarketsResponse> ListMarketsAsync(HistoricalMarketQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Gets one archived market.</summary>
    Task<MarketResponse> GetMarketAsync(string ticker, CancellationToken cancellationToken = default);

    /// <summary>Gets candlesticks for an archived market.</summary>
    Task<HistoricalMarketCandlesticksResponse> GetMarketCandlesticksAsync(string ticker, MarketCandlesticksQuery query, CancellationToken cancellationToken = default);

    /// <summary>Lists archived public trades.</summary>
    Task<TradesResponse> ListTradesAsync(HistoricalTradeQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Lists archived user fills.</summary>
    Task<FillsResponse> ListFillsAsync(HistoricalFillQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Lists archived user orders.</summary>
    Task<OrdersResponse> ListOrdersAsync(HistoricalOrderQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Lists archived settled positions.</summary>
    Task<PositionsResponse> ListPositionsAsync(HistoricalPositionQuery? query = null, CancellationToken cancellationToken = default);
}
