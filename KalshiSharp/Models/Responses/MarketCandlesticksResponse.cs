using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Candlesticks for one market.
/// </summary>
public sealed record MarketCandlesticksResponse
{
        /// <summary>
        /// Unique identifier for the market.
        /// </summary>
        public required string Ticker { get; init; }

        /// <summary>
        /// Array of candlestick data points for the specified time range.
        /// </summary>
        public IReadOnlyList<Candlestick> Candlesticks { get; init; } = [];

        public sealed record Candlestick
        {
            /// <summary>
            /// Unix timestamp for the inclusive end of the candlestick period.
            /// </summary>
            [JsonPropertyName("end_period_ts")]
            public long EndPeriodTimestamp { get; init; }

            /// <summary>
            /// Open, high, low, close (OHLC) data for YES buy offers on the market during the candlestick period.
            /// </summary>
            public required Ohlc YesBid { get; init; }

            /// <summary>
            /// Open, high, low, close (OHLC) data for YES sell offers on the market during the candlestick period.
            /// </summary>
            public required Ohlc YesAsk { get; init; }

            /// <summary>
            /// Open, high, low, close (OHLC) and more data for trade YES contract prices on the market during the candlestick period.
            /// </summary>
            public required PriceOhlc Price { get; init; }

            /// <summary>
            /// String representation of the number of contracts bought on the market during the candlestick period.
            /// </summary>
            public required string VolumeFp { get; init; }

            /// <summary>
            /// String representation of the number of contracts bought on the market by end of the candlestick period (end_period_ts).
            /// </summary>
            public required string OpenInterestFp { get; init; }

            public sealed record Ohlc
            {
                /// <summary>
                /// Offer price on the market at the start of the candlestick period (in dollars).
                /// </summary>
                public required string OpenDollars { get; init; }

                /// <summary>
                /// Lowest offer price on the market during the candlestick period (in dollars).
                /// </summary>
                public required string LowDollars { get; init; }

                /// <summary>
                /// Highest offer price on the market during the candlestick period (in dollars).
                /// </summary>
                public required string HighDollars { get; init; }

                /// <summary>
                /// Offer price on the market at the end of the candlestick period (in dollars).
                /// </summary>
                public required string CloseDollars { get; init; }
            }

            public sealed record PriceOhlc
            {
                /// <summary>
                /// First traded YES contract price on the market during the candlestick period (in dollars). May be null if there was no trade during the period.
                /// </summary>
                public string? OpenDollars { get; init; }

                /// <summary>
                /// Lowest traded YES contract price on the market during the candlestick period (in dollars). May be null if there was no trade during the period.
                /// </summary>
                public string? LowDollars { get; init; }

                /// <summary>
                /// Highest traded YES contract price on the market during the candlestick period (in dollars). May be null if there was no trade during the period.
                /// </summary>
                public string? HighDollars { get; init; }

                /// <summary>
                /// Last traded YES contract price on the market during the candlestick period (in dollars). May be null if there was no trade during the period.
                /// </summary>
                public string? CloseDollars { get; init; }

                /// <summary>
                /// Mean traded YES contract price on the market during the candlestick period (in dollars). May be null if there was no trade during the period.
                /// </summary>
                public string? MeanDollars { get; init; }

                /// <summary>
                /// Last traded YES contract price on the market before the candlestick period (in dollars). May be null if there were no trades before the period.
                /// </summary>
                public string? PreviousDollars { get; init; }

                /// <summary>
                /// String representation of the number of contracts bought on the market during the candlestick period.
                /// </summary>
                public string? MinDollars { get; init; }

                /// <summary>
                /// String representation of the number of contracts bought on the market by end of the candlestick period (end_period_ts).
                /// </summary>
                public string? MaxDollars { get; init; }
            }
        }
}
