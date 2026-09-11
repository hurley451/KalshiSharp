using System.Globalization;
using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Query parameters for listing milestones.
/// </summary>
public sealed record MilestoneQuery
{
    /// <summary>Number of milestones to return per page.</summary>
    public required int Limit { get; init; }

    /// <summary>Filters milestones starting on or after this time.</summary>
    public DateTimeOffset? MinimumStartDate { get; init; }

    /// <summary>Filters by milestone category.</summary>
    public string? Category { get; init; }

    /// <summary>Filters by competition.</summary>
    public string? Competition { get; init; }

    /// <summary>Filters by provider source ID.</summary>
    public string? SourceId { get; init; }

    /// <summary>Filters by milestone type.</summary>
    public string? Type { get; init; }

    /// <summary>Filters by a related event ticker.</summary>
    public string? RelatedEventTicker { get; init; }

    /// <summary>Cursor returned by the previous page.</summary>
    public string? Cursor { get; init; }

    /// <summary>Only returns milestones updated after this time.</summary>
    public DateTimeOffset? MinUpdatedTs { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    /// <returns>The query string including the leading question mark.</returns>
    public string ToQueryString()
    {
        if (Limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(Limit), Limit, "Limit must be between 1 and 500.");
        }

        var builder = new QueryStringBuilder();
        builder.Append("limit", Limit.ToString(CultureInfo.InvariantCulture));

        if (MinimumStartDate.HasValue)
        {
            builder.Append(
                "minimum_start_date",
                MinimumStartDate.Value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        }

        builder.AppendIfNotEmpty("category", Category);
        builder.AppendIfNotEmpty("competition", Competition);
        builder.AppendIfNotEmpty("source_id", SourceId);
        builder.AppendIfNotEmpty("type", Type);
        builder.AppendIfNotEmpty("related_event_ticker", RelatedEventTicker);
        builder.AppendIfNotEmpty("cursor", Cursor);

        if (MinUpdatedTs.HasValue)
        {
            builder.Append(
                "min_updated_ts",
                MinUpdatedTs.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }

        return builder.Build();
    }
}
