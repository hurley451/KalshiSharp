using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Markets;

/// <summary>
/// Client for Kalshi market endpoints.
/// </summary>
public interface IMarketClient
{
    /// <summary>
    /// Gets a single market by ticker.
    /// </summary>
    /// <param name="ticker">The market ticker.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The market details.</returns>
    Task<MarketResponse> GetMarketAsync(string ticker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists markets with optional filtering and pagination.
    /// </summary>
    /// <param name="query">Optional query parameters for filtering and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of markets.</returns>
    Task<MarketsResponse> ListMarketsAsync(MarketQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the order book for a market.
    /// </summary>
    /// <param name="ticker">The market ticker.</param>
    /// <param name="depth">Optional depth of order book levels to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order book for the market.</returns>
    Task<OrderBookResponse> GetOrderBookAsync(string ticker, int? depth = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trades for a market.
    /// </summary>
    /// <param name="ticker">The market ticker.</param>
    /// <param name="cursor">Optional cursor for pagination.</param>
    /// <param name="limit">Optional limit on number of trades to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of trades.</returns>
    Task<TradesResponse> GetTradesAsync(string ticker, string? cursor = null, int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets candlesticks for a market.
    /// </summary>
    /// <param name="seriesTicker">Series ticker - the series that contains the target market</param>
    /// <param name="ticker">Market ticker - unique identifier for the specific market</param>
    /// <param name="query">Query parameters</param>
    ///  /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<MarketCandlesticksResponse> GetMarketCandlesticks(string seriesTicker, string ticker, MarketCandlesticksQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets candlesticks for multiple markets in a single request.
    /// Accepts up to 100 market tickers and returns up to 10,000 candlesticks total.
    /// </summary>
    /// <param name="query">Query parameters including the list of market tickers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Candlestick data grouped by market ticker.</returns>
    Task<BatchMarketCandlesticksResponse> GetBatchMarketCandlesticksAsync(BatchMarketCandlesticksQuery query, CancellationToken cancellationToken = default);
}
