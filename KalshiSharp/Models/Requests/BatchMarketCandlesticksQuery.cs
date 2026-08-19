using KalshiSharp.Models.Common;
using KalshiSharp.Models.Enums;
using System.Globalization;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Query parameters for the batch market candlesticks endpoint.
/// Accepts up to 100 market tickers per request.
/// </summary>
public sealed record BatchMarketCandlesticksQuery
{
    /// <summary>
    /// The market tickers to retrieve candlesticks for. Maximum 100.
    /// </summary>
    public required IReadOnlyList<string> MarketTickers { get; init; }

    /// <summary>
    /// Start timestamp. Candlesticks will include those ending on or after this time.
    /// </summary>
    public required DateTimeOffset StartTimestamp { get; init; }

    /// <summary>
    /// End timestamp. Candlesticks will include those ending on or before this time.
    /// </summary>
    public required DateTimeOffset EndTimestamp { get; init; }

    /// <summary>
    /// Time period length of each candlestick in minutes. Valid values are 1 (1 minute), 60 (1 hour), or 1440 (1 day).
    /// </summary>
    /// <remarks>
    /// Available options: 1, 60, 1440
    /// </remarks>
    public required PeriodInterval PeriodInterval { get; init; }

    /// <summary>
    /// If true, prepends the latest candlestick available before the start_ts for each market.
    /// </summary>
    /// <remarks>
    /// This synthetic candlestick is created by:
    /// 1. Finding the most recent real candlestick before start_ts
    /// 2. Projecting it forward to the first period boundary (calculated as the next period interval after start_ts)
    /// 3. Setting all OHLC prices to null, and previous_price to the close price from the real candlestick
    /// </remarks>
    public bool IncludeLatestBeforeStart { get; init; }

    /// <summary>
    /// Builds the query string for the API request.
    /// </summary>
    /// <returns>The query string including the leading '?' if parameters exist.</returns>
    public string ToQueryString()
    {
        if (MarketTickers.Count is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(MarketTickers), "Between 1 and 100 market tickers are required.");
        }

        if (MarketTickers.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Market tickers cannot be empty.", nameof(MarketTickers));
        }

        if (EndTimestamp < StartTimestamp)
        {
            throw new ArgumentException("EndTimestamp must be on or after StartTimestamp.");
        }

        if (!Enum.IsDefined(PeriodInterval))
        {
            throw new ArgumentOutOfRangeException(nameof(PeriodInterval));
        }

        var builder = new QueryStringBuilder();

        builder.Append("market_tickers", string.Join(",", MarketTickers));
        builder.Append("start_ts", StartTimestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        builder.Append("end_ts", EndTimestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        builder.Append("period_interval", ((int)PeriodInterval).ToString(CultureInfo.InvariantCulture));
        if (IncludeLatestBeforeStart)
        {
            builder.Append("include_latest_before_start", "true");
        }

        return builder.Build();
    }
}
