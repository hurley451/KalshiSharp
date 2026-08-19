using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>Candlesticks for an archived market.</summary>
public sealed record HistoricalMarketCandlesticksResponse
{
    /// <summary>Market ticker.</summary>
    public required string Ticker { get; init; }

    /// <summary>Archived candlesticks.</summary>
    public IReadOnlyList<HistoricalCandlestick> Candlesticks { get; init; } = [];

    /// <summary>An archived candlestick.</summary>
    public sealed record HistoricalCandlestick
    {
        /// <summary>Inclusive period-end Unix timestamp.</summary>
        [JsonPropertyName("end_period_ts")]
        public long EndPeriodTimestamp { get; init; }

        /// <summary>YES bid price values.</summary>
        public required HistoricalOhlc YesBid { get; init; }

        /// <summary>YES ask price values.</summary>
        public required HistoricalOhlc YesAsk { get; init; }

        /// <summary>Trade price values.</summary>
        public required HistoricalPriceOhlc Price { get; init; }

        /// <summary>Traded quantity.</summary>
        public required string Volume { get; init; }

        /// <summary>Open interest.</summary>
        public required string OpenInterest { get; init; }
    }

    /// <summary>Archived open, low, high, and close values.</summary>
    public sealed record HistoricalOhlc
    {
        /// <summary>Opening value.</summary>
        public required string Open { get; init; }

        /// <summary>Lowest value.</summary>
        public required string Low { get; init; }

        /// <summary>Highest value.</summary>
        public required string High { get; init; }

        /// <summary>Closing value.</summary>
        public required string Close { get; init; }
    }

    /// <summary>Archived trade-price values.</summary>
    public sealed record HistoricalPriceOhlc
    {
        /// <summary>Opening value.</summary>
        public string? Open { get; init; }

        /// <summary>Lowest value.</summary>
        public string? Low { get; init; }

        /// <summary>Highest value.</summary>
        public string? High { get; init; }

        /// <summary>Closing value.</summary>
        public string? Close { get; init; }

        /// <summary>Mean value.</summary>
        public string? Mean { get; init; }

        /// <summary>Previous value.</summary>
        public string? Previous { get; init; }
    }
}
