namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response from the batch market candlesticks endpoint.
/// Contains candlestick data for multiple markets, grouped by market ticker.
/// Returns up to 10,000 candlesticks total across all markets.
/// </summary>
public sealed record BatchMarketCandlesticksResponse
{
    /// <summary>
    /// Array of market candlestick data, one entry per requested market.
    /// </summary>
    public IReadOnlyList<MarketCandlesticks> Markets { get; init; } = [];

    /// <summary>
    /// Candlestick data for a single market.
    /// </summary>
    public sealed record MarketCandlesticks
    {
        /// <summary>
        /// Market ticker string (e.g., 'INXD-24JAN01').
        /// </summary>
        public required string MarketTicker { get; init; }

        /// <summary>
        /// Array of candlestick data points for this market.
        /// </summary>
        public IReadOnlyList<MarketCandlesticksResponse.Candlestick> Candlesticks { get; init; } = [];
    }
}
