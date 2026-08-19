using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Query for retrieving multiple market order books.</summary>
public sealed record MultipleOrderBooksQuery
{
    /// <summary>Market tickers to retrieve. Kalshi accepts between 1 and 100.</summary>
    public required IReadOnlyList<string> Tickers { get; init; }

    /// <summary>Optional order-book depth.</summary>
    public int? Depth { get; init; }

    internal string ToQueryString()
    {
        if (Tickers.Count is < 1 or > 100 || Tickers.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Between 1 and 100 non-empty tickers are required.", nameof(Tickers));
        }
        if (Depth is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Depth));
        }

        var builder = new QueryStringBuilder();
        foreach (var ticker in Tickers)
        {
            builder.Append("tickers", ticker);
        }
        if (Depth.HasValue)
        {
            builder.Append("depth", Depth.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return builder.Build();
    }
}
