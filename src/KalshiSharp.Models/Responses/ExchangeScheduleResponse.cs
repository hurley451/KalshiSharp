namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents the trading schedule of the Kalshi exchange.
/// </summary>
public sealed record ExchangeScheduleResponse
{
    /// <summary>
    /// The schedule entries for the exchange.
    /// </summary>
    public required IReadOnlyList<ScheduleEntry> Schedule { get; init; }
}

/// <summary>
/// Represents a single schedule entry with start and end times.
/// </summary>
public sealed record ScheduleEntry
{
    /// <summary>
    /// When this schedule period starts.
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// When this schedule period ends.
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Description of this schedule period.
    /// </summary>
    public string? Maintenance { get; init; }
}
