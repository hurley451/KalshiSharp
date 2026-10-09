using System.Globalization;
using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Query parameters for listing fills.</summary>
public sealed record FillQuery : PaginationParameters
{
    /// <summary>Filter by a single market ticker.</summary>
    public string? Ticker { get; init; }

    /// <summary>Filter by up to 100 market tickers.</summary>
    public IReadOnlyList<string>? Tickers { get; init; }

    /// <summary>Filter by order identifier.</summary>
    public string? OrderId { get; init; }

    /// <summary>Return fills after this time.</summary>
    public DateTimeOffset? MinTime { get; init; }

    /// <summary>Return fills before this time.</summary>
    public DateTimeOffset? MaxTime { get; init; }

    /// <summary>Filter by subaccount, including zero for the primary account.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Filter by exchange index when shard-aware reads are active.</summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>Builds the query string.</summary>
    public string ToQueryString()
    {
        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        AppendTickerFilter(builder, Ticker, Tickers);
        builder.AppendIfNotEmpty("order_id", OrderId);
        if (Subaccount.HasValue)
        {
            builder.Append("subaccount", Subaccount.Value.ToString(CultureInfo.InvariantCulture));
        }
        if (ExchangeIndex.HasValue)
        {
            builder.Append("exchange_index", ExchangeIndex.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (MinTime.HasValue)
        {
            builder.Append("min_ts", MinTime.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }

        if (MaxTime.HasValue)
        {
            builder.Append("max_ts", MaxTime.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }

        return builder.Build();
    }

    private static void AppendTickerFilter(QueryStringBuilder builder, string? ticker, IReadOnlyList<string>? tickers)
    {
        if (!string.IsNullOrEmpty(ticker) && tickers is { Count: > 0 })
        {
            throw new ArgumentException("Specify either Ticker or Tickers, not both.", nameof(tickers));
        }

        if (tickers is { Count: > 0 })
        {
            if (tickers.Count > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(tickers), "At most 100 market tickers can be supplied.");
            }

            if (tickers.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Market tickers cannot contain empty values.", nameof(tickers));
            }

            builder.Append("ticker", string.Join(",", tickers));
            return;
        }

        builder.AppendIfNotEmpty("ticker", ticker);
    }
}
