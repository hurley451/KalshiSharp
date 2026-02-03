namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents an event containing one or more markets.
/// </summary>
public sealed record EventResponse
{
    /// <summary>
    /// Unique ticker identifier for this event.
    /// </summary>
    public required string EventTicker { get; init; }

    /// <summary>
    /// Human-readable title for this event.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Subtitle providing additional context.
    /// </summary>
    public string? Subtitle { get; init; }

    /// <summary>
    /// Category this event belongs to.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Mutually exclusive status ('true' for mutually exclusive).
    /// </summary>
    public bool? MutuallyExclusive { get; init; }   

    /// <summary>
    /// Markets belonging to this event.
    /// </summary>
    public IReadOnlyList<MarketResponse>? Markets { get; init; }

    /// <summary>
    /// When this event was created.
    /// </summary>
    public DateTimeOffset? CreatedTime { get; init; }

    /// <summary>
    /// When this event closes.
    /// </summary>
    public DateTimeOffset? CloseTime { get; init; }

    /// <summary>
    /// Series ticker if this event is part of a series.
    /// </summary>
    public string? SeriesTicker { get; init; }
}
