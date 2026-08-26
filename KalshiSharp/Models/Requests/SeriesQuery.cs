using System.Globalization;
using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Query parameters for listing series.
/// </summary>
public sealed record SeriesQuery
{
    /// <summary>Filter by category.</summary>
    public string? Category { get; init; }

    /// <summary>Filter by tags. Values are sent as a comma-separated list.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Whether to include product-specific metadata.</summary>
    public bool? IncludeProductMetadata { get; init; }

    /// <summary>Whether to include aggregate series volume.</summary>
    public bool? IncludeVolume { get; init; }

    /// <summary>Only return series updated after this time.</summary>
    public DateTimeOffset? MinUpdatedTs { get; init; }

    /// <summary>
    /// Builds the query string for the API request.
    /// </summary>
    /// <returns>The query string including the leading '?' if parameters exist.</returns>
    public string ToQueryString()
    {
        var builder = new QueryStringBuilder();

        builder.AppendIfNotEmpty("category", Category);

        if (Tags is { Count: > 0 })
        {
            builder.Append("tags", string.Join(",", Tags));
        }

        if (IncludeProductMetadata.HasValue)
        {
            builder.Append("include_product_metadata", IncludeProductMetadata.Value ? "true" : "false");
        }

        if (IncludeVolume.HasValue)
        {
            builder.Append("include_volume", IncludeVolume.Value ? "true" : "false");
        }

        if (MinUpdatedTs.HasValue)
        {
            builder.Append(
                "min_updated_ts",
                MinUpdatedTs.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }

        return builder.Build();
    }
}
