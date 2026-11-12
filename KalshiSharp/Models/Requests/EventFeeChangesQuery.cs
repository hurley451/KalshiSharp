using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Filters for one page of event fee changes.</summary>
public sealed record EventFeeChangesQuery : PaginationParameters
{
    /// <summary>Restricts results to this event ticker.</summary>
    public string? EventTicker { get; init; }
    /// <summary>Builds the escaped query string. Limit must be 1..1000; omission uses the API default (100).</summary>
    public string ToQueryString()
    {
        ValidateLimit(Limit);
        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.AppendIfNotEmpty("event_ticker", EventTicker);
        return builder.Build();
    }
    private static void ValidateLimit(int? limit)
    {
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be between 1 and 1000.");
    }
}
