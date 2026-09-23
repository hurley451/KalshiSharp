using System.Globalization;
using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Query parameters for archived markets.</summary>
public sealed record HistoricalMarketQuery : PaginationParameters
{
    /// <summary>Comma-separated market tickers.</summary>
    public IReadOnlyList<string>? Tickers { get; init; }

    /// <summary>Event ticker filter.</summary>
    public string? EventTicker { get; init; }

    /// <summary>Series ticker filter.</summary>
    public string? SeriesTicker { get; init; }

    /// <summary>Multivariate filter, such as <c>exclude</c>.</summary>
    public string? MveFilter { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        if (Tickers is { Count: > 0 })
        {
            builder.Append("tickers", string.Join(',', Tickers));
        }

        builder.AppendIfNotEmpty("event_ticker", EventTicker);
        builder.AppendIfNotEmpty("series_ticker", SeriesTicker);
        builder.AppendIfNotEmpty("mve_filter", MveFilter);
        return builder.Build();
    }
}

/// <summary>Query parameters for archived trades.</summary>
public sealed record HistoricalTradeQuery : PaginationParameters
{
    /// <summary>Market ticker filter.</summary>
    public string? Ticker { get; init; }

    /// <summary>Minimum trade timestamp.</summary>
    public DateTimeOffset? MinTimestamp { get; init; }

    /// <summary>Maximum trade timestamp.</summary>
    public DateTimeOffset? MaxTimestamp { get; init; }

    /// <summary>Optional block-trade filter.</summary>
    public bool? IsBlockTrade { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.AppendIfNotEmpty("ticker", Ticker);
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

/// <summary>Query parameters for archived fills.</summary>
public sealed record HistoricalFillQuery : PaginationParameters
{
    /// <summary>Market ticker filter.</summary>
    public string? Ticker { get; init; }

    /// <summary>Minimum fill timestamp.</summary>
    public DateTimeOffset? MinTimestamp { get; init; }

    /// <summary>Maximum fill timestamp.</summary>
    public DateTimeOffset? MaxTimestamp { get; init; }

    /// <summary>Optional subaccount filter. Omission returns fills for all permitted subaccounts.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString() => BuildTickerTimestampAndSubaccountQuery(this, Ticker, MinTimestamp, MaxTimestamp, Subaccount);

    internal static string BuildTickerTimestampAndSubaccountQuery(
        PaginationParameters pagination,
        string? ticker,
        DateTimeOffset? minTimestamp,
        DateTimeOffset? maxTimestamp,
        int? subaccount)
    {
        if (subaccount is < 0 or > 63)
        {
            throw new ArgumentOutOfRangeException(
                nameof(subaccount),
                subaccount,
                "Subaccount must be between 0 and 63.");
        }

        var builder = new QueryStringBuilder();
        if (pagination.Limit.HasValue)
        {
            builder.Append("limit", pagination.Limit.Value.ToString(CultureInfo.InvariantCulture));
        }

        builder.AppendIfNotEmpty("cursor", pagination.Cursor);
        builder.AppendIfNotEmpty("ticker", ticker);
        if (minTimestamp.HasValue)
        {
            builder.Append("min_ts", minTimestamp.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }

        if (maxTimestamp.HasValue)
        {
            builder.Append("max_ts", maxTimestamp.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }

        if (subaccount.HasValue)
        {
            builder.Append("subaccount", subaccount.Value.ToString(CultureInfo.InvariantCulture));
        }

        return builder.Build();
    }
}

/// <summary>Query parameters for archived orders.</summary>
public sealed record HistoricalOrderQuery : PaginationParameters
{
    /// <summary>Market ticker filter.</summary>
    public string? Ticker { get; init; }

    /// <summary>Minimum order-update timestamp.</summary>
    public DateTimeOffset? MinTimestamp { get; init; }

    /// <summary>Maximum order-update timestamp.</summary>
    public DateTimeOffset? MaxTimestamp { get; init; }

    /// <summary>Optional subaccount filter. Omission returns orders for all permitted subaccounts.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString() => HistoricalFillQuery.BuildTickerTimestampAndSubaccountQuery(
        this,
        Ticker,
        MinTimestamp,
        MaxTimestamp,
        Subaccount);
}

/// <summary>Query parameters for archived positions.</summary>
public sealed record HistoricalPositionQuery : PaginationParameters
{
    /// <summary>Market ticker filter.</summary>
    public string? Ticker { get; init; }

    /// <summary>Event ticker filter.</summary>
    public string? EventTicker { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.AppendIfNotEmpty("ticker", Ticker);
        builder.AppendIfNotEmpty("event_ticker", EventTicker);
        return builder.Build();
    }
}
