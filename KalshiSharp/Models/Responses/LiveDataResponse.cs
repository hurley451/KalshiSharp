using System.Text.Json;
using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>Response envelope for one live-data payload.</summary>
public sealed record LiveDataResponse
{
    /// <summary>Live-data payload.</summary>
    public required LiveDataPayload LiveData { get; init; }
}

/// <summary>Response envelope for batch live-data payloads.</summary>
public sealed record LiveDataBatchResponse
{
    /// <summary>Live-data payloads.</summary>
    public required IReadOnlyList<LiveDataPayload> LiveDatas { get; init; }
}

/// <summary>Event-keyed live-data response envelope.</summary>
public sealed record EventLiveDataResponse
{
    /// <summary>Event live-data payload.</summary>
    public required EventLiveDataPayload LiveData { get; init; }
}

/// <summary>One live-data payload.</summary>
public sealed record LiveDataPayload
{
    /// <summary>Live-data schema type.</summary>
    public required string Type { get; init; }

    /// <summary>Provider or sport-specific details.</summary>
    public required JsonElement Details { get; init; }

    /// <summary>Milestone identifier.</summary>
    public required string MilestoneId { get; init; }

    /// <summary>Additional payload fields returned by Kalshi.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

/// <summary>Event-keyed live-data payload.</summary>
public sealed record EventLiveDataPayload
{
    /// <summary>Live-data schema type.</summary>
    public required string Type { get; init; }

    /// <summary>Provider or event-specific details.</summary>
    public required JsonElement Details { get; init; }

    /// <summary>Whether this payload represents historical data.</summary>
    public bool? IsHistorical { get; init; }

    /// <summary>Default display range for this event live data.</summary>
    public string? DefaultRange { get; init; }

    /// <summary>Available range options.</summary>
    public IReadOnlyList<string>? RangeOptions { get; init; }

    /// <summary>Additional payload fields returned by Kalshi.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

/// <summary>Play-by-play game statistics response.</summary>
public sealed record GameStatsResponse
{
    /// <summary>Raw play-by-play payload. May be null for unsupported milestones.</summary>
    public required JsonElement Pbp { get; init; }

    /// <summary>Additional response fields returned by Kalshi.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
