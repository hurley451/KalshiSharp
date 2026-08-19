using System.Globalization;
using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Query parameters for listing fills.</summary>
public sealed record FillQuery : PaginationParameters
{
    /// <summary>Filter by market ticker.</summary>
    public string? Ticker { get; init; }

    /// <summary>Filter by order identifier.</summary>
    public string? OrderId { get; init; }

    /// <summary>Return fills after this time.</summary>
    public DateTimeOffset? MinTime { get; init; }

    /// <summary>Return fills before this time.</summary>
    public DateTimeOffset? MaxTime { get; init; }

    /// <summary>Filter by subaccount, including zero for the primary account.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Builds the query string.</summary>
    public string ToQueryString()
    {
        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.AppendIfNotEmpty("ticker", Ticker);
        builder.AppendIfNotEmpty("order_id", OrderId);
        if (Subaccount.HasValue)
        {
            builder.Append("subaccount", Subaccount.Value.ToString(CultureInfo.InvariantCulture));
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
}
