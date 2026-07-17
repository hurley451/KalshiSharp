using KalshiSharp.Models.Common;
using KalshiSharp.Models.Enums;
using System.Globalization;

namespace KalshiSharp.Models.Requests
{
    /// <summary>
    /// Path and Query parameters for fetching market candlesticks
    /// </summary>   
    public sealed record MarketCandlesticksQuery
    {
        /// <summary>
        /// Start timestamp (Unix timestamp). Candlesticks will include those ending on or after this time.
        /// </summary>       
        public required DateTimeOffset StartTimestamp { get; init; }

        /// <summary>
        /// End timestamp (Unix timestamp). Candlesticks will include those ending on or before this time.
        /// </summary>       
        public required DateTimeOffset EndTimestamp { get; init; }

        /// <summary>
        /// Time period length of each candlestick in minutes. Valid values are 1 (1 minute), 60 (1 hour), or 1440 (1 day).
        /// </summary>
        /// <remarks>
        /// Available options: 1, 60, 1440  
        /// </remarks>       
        public PeriodInterval PeriodInterval { get; init; }

        /// <summary>
        /// If true, prepends the latest candlestick available before the start_ts.
        /// </summary>
        /// <remarks>
        /// This synthetic candlestick is created by:
        /// 1. Finding the most recent real candlestick before start_ts
        /// 2. Projecting it forward to the first period boundary(calculated as the next period interval after start_ts)
        /// 3. Setting all OHLC prices to null, and previous_price to the close price from the real candlestick
        /// </remarks>       
        public bool IncludeLatestBeforeStart { get; init; }

        /// <summary>
        /// Builds the query string for the API request.
        /// </summary>
        /// <returns>The query string including the leading '?' if parameters exist.</returns>
        public string ToQueryString()
        {
            var builder = new QueryStringBuilder();                                

            builder.Append("start_ts", StartTimestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));            
            builder.Append("end_ts", EndTimestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
            builder.Append("period_interval", ((int)PeriodInterval).ToString(CultureInfo.InvariantCulture));
            builder.AppendIfNotEmpty("include_latest_before_start", IncludeLatestBeforeStart.ToString().ToLowerInvariant());

            return builder.Build();
        }
    }
}
