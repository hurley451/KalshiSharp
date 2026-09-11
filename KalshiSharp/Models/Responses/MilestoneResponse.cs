using System.Text.Json;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents a Kalshi milestone.
/// </summary>
public sealed record MilestoneResponse
{
    /// <summary>Unique milestone ID.</summary>
    public required string Id { get; init; }

    /// <summary>Milestone category.</summary>
    public required string Category { get; init; }

    /// <summary>Milestone type.</summary>
    public required string Type { get; init; }

    /// <summary>Milestone start time.</summary>
    public required DateTimeOffset StartDate { get; init; }

    /// <summary>Related event tickers.</summary>
    public required IReadOnlyList<string> RelatedEventTickers { get; init; }

    /// <summary>Human-readable milestone title.</summary>
    public required string Title { get; init; }

    /// <summary>Notification text for the milestone.</summary>
    public required string NotificationMessage { get; init; }

    /// <summary>Type-specific milestone details.</summary>
    public required JsonElement Details { get; init; }

    /// <summary>Event tickers directly related to the milestone outcome.</summary>
    public required IReadOnlyList<string> PrimaryEventTickers { get; init; }

    /// <summary>When the milestone was last updated.</summary>
    public required DateTimeOffset LastUpdatedTs { get; init; }

    /// <summary>Milestone end time when supplied.</summary>
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>Primary external provider ID when supplied.</summary>
    public string? SourceId { get; init; }

    /// <summary>External provider IDs keyed by source name.</summary>
    public IReadOnlyDictionary<string, string>? SourceIds { get; init; }
}
