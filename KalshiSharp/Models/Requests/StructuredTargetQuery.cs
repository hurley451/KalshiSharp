using System.Globalization;
using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Query parameters for listing structured targets.
/// </summary>
public sealed record StructuredTargetQuery
{
    /// <summary>Filters by target IDs. Each ID is emitted as a separate query parameter.</summary>
    public IReadOnlyList<string>? Ids { get; init; }

    /// <summary>Filters by structured-target type.</summary>
    public string? Type { get; init; }

    /// <summary>Filters by competition, league, conference, division, or tour.</summary>
    public string? Competition { get; init; }

    /// <summary>Number of targets to return per page.</summary>
    public int? PageSize { get; init; }

    /// <summary>Cursor returned by the previous page.</summary>
    public string? Cursor { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    /// <returns>The query string including the leading question mark when parameters exist.</returns>
    public string ToQueryString()
    {
        if (Ids is { Count: > 2000 })
        {
            throw new ArgumentOutOfRangeException(nameof(Ids), Ids.Count, "At most 2000 target IDs may be requested.");
        }

        if (PageSize is < 1 or > 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(PageSize), PageSize, "Page size must be between 1 and 2000.");
        }

        var builder = new QueryStringBuilder();
        if (Ids is { Count: > 0 })
        {
            foreach (var id in Ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("Target IDs cannot contain null, empty, or whitespace values.", nameof(Ids));
                }

                builder.Append("ids", id);
            }
        }

        builder.AppendIfNotEmpty("type", Type);
        builder.AppendIfNotEmpty("competition", Competition);

        if (PageSize.HasValue)
        {
            builder.Append("page_size", PageSize.Value.ToString(CultureInfo.InvariantCulture));
        }

        builder.AppendIfNotEmpty("cursor", Cursor);
        return builder.Build();
    }
}
