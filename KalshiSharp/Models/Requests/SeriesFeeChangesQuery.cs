using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Optional filters for series fee changes. This endpoint is not paginated.</summary>
public sealed record SeriesFeeChangesQuery
{
    /// <summary>Restricts results to this series ticker.</summary>
    public string? SeriesTicker { get; init; }
    /// <summary>Includes historical changes when true. Omission uses the API default (false).</summary>
    public bool? ShowHistorical { get; init; }
    /// <summary>Builds the escaped query string.</summary>
    public string ToQueryString()
    {
        var builder = new QueryStringBuilder();
        builder.AppendIfNotEmpty("series_ticker", SeriesTicker);
        if (ShowHistorical.HasValue) builder.Append("show_historical", ShowHistorical.Value ? "true" : "false");
        return builder.Build();
    }
}
