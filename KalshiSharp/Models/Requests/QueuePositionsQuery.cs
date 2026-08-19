using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Filters for listing order queue positions.</summary>
public sealed record QueuePositionsQuery
{
    /// <summary>Optional market tickers.</summary>
    public IReadOnlyList<string> MarketTickers { get; init; } = [];

    /// <summary>Optional event ticker.</summary>
    public string? EventTicker { get; init; }

    /// <summary>Optional subaccount number.</summary>
    public int? Subaccount { get; init; }

    internal string ToQueryString()
    {
        if (Subaccount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Subaccount));
        }

        var builder = new QueryStringBuilder();
        foreach (var ticker in MarketTickers)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                throw new ArgumentException("Market tickers cannot contain empty values.", nameof(MarketTickers));
            }
        }
        if (MarketTickers.Count > 0)
        {
            builder.Append("market_tickers", string.Join(',', MarketTickers));
        }
        builder.AppendIfNotEmpty("event_ticker", EventTicker);
        if (Subaccount.HasValue)
        {
            builder.Append("subaccount", Subaccount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return builder.Build();
    }
}
