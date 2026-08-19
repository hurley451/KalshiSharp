using System.Globalization;
using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Query parameters for public market trades.</summary>
public sealed record MarketTradeQuery : PaginationParameters
{
    /// <summary>Market ticker filter.</summary>
    public required string Ticker { get; init; }

    /// <summary>Minimum trade timestamp.</summary>
    public DateTimeOffset? MinTimestamp { get; init; }

    /// <summary>Maximum trade timestamp.</summary>
    public DateTimeOffset? MaxTimestamp { get; init; }

    /// <summary>Optional block-trade filter.</summary>
    public bool? IsBlockTrade { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Ticker);
        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.Append("ticker", Ticker);
        AppendTimestamp(builder, "min_ts", MinTimestamp);
        AppendTimestamp(builder, "max_ts", MaxTimestamp);
        if (IsBlockTrade.HasValue)
        {
            builder.Append("is_block_trade", IsBlockTrade.Value ? "true" : "false");
        }

        return builder.Build();
    }

    private static void AppendTimestamp(QueryStringBuilder builder, string name, DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            builder.Append(name, value.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }
    }
}
