using System.Globalization;
using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Query parameters for listing positions.</summary>
public sealed record PositionQuery : PaginationParameters
{
    /// <summary>Filter by market ticker.</summary>
    public string? Ticker { get; init; }

    /// <summary>Filter by event ticker.</summary>
    public string? EventTicker { get; init; }

    /// <summary>
    /// Restrict results to positions with non-zero values in these fields.
    /// Supported values are <c>position</c> and <c>total_traded</c>.
    /// </summary>
    public IReadOnlyList<string>? CountFilter { get; init; }

    /// <summary>Filter by subaccount, including zero for the primary account.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Builds the query string.</summary>
    public string ToQueryString()
    {
        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.AppendIfNotEmpty("ticker", Ticker);
        builder.AppendIfNotEmpty("event_ticker", EventTicker);
        if (Subaccount.HasValue)
        {
            builder.Append("subaccount", Subaccount.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (CountFilter is { Count: > 0 })
        {
            builder.Append("count_filter", string.Join(",", CountFilter));
        }

        return builder.Build();
    }
}
