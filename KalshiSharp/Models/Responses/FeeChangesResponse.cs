using System.Text.Json;
using System.Text.Json.Serialization;
using KalshiSharp.Serialization.Converters;

namespace KalshiSharp.Models.Responses;

/// <summary>Series fee changes. An empty collection is distinct from malformed or omitted data.</summary>
public sealed record SeriesFeeChangesResponse : IJsonOnDeserialized
{
    [JsonPropertyName("series_fee_change_arr")]
    public required IReadOnlyList<SeriesFeeChange> SeriesFeeChanges { get; init; }
    void IJsonOnDeserialized.OnDeserialized()
    {
        if (SeriesFeeChanges is null || SeriesFeeChanges.Any(item => item is null)) throw new JsonException("Invalid series fee changes collection.");
    }
}

/// <summary>A scheduled series fee change, not a calculated fee quote.</summary>
public sealed record SeriesFeeChange : IJsonOnDeserialized
{
    public required string Id { get; init; }
    public required string SeriesTicker { get; init; }
    /// <summary>Wire fee type, retained verbatim including future values.</summary>
    public required string FeeType { get; init; }
    [JsonConverter(typeof(FeeMultiplierConverter))]
    public required decimal FeeMultiplier { get; init; }
    [JsonConverter(typeof(FeeTimestampConverter))]
    public required DateTimeOffset ScheduledTs { get; init; }
    void IJsonOnDeserialized.OnDeserialized()
    {
        if (Id is null || SeriesTicker is null || FeeType is null) throw new JsonException("Invalid series fee change identity or type.");
    }
}

/// <summary>One page of event fee changes. Follow the opaque cursor until it is empty.</summary>
public sealed record EventFeeChangesResponse : IJsonOnDeserialized
{
    public required IReadOnlyList<EventFeeChange> EventFeeChanges { get; init; }
    /// <summary>Required opaque continuation cursor; empty means no more results.</summary>
    public required string Cursor { get; init; }
    void IJsonOnDeserialized.OnDeserialized()
    {
        if (EventFeeChanges is null || EventFeeChanges.Any(item => item is null) || Cursor is null) throw new JsonException("Invalid event fee changes page.");
    }
}

/// <summary>A scheduled event override. Null values clear the corresponding override to its parent series value.</summary>
public sealed record EventFeeChange : IJsonOnDeserialized
{
    public required string Id { get; init; }
    public required string EventTicker { get; init; }
    public required string SeriesTicker { get; init; }
    /// <summary>Required nullable wire fee type, retained verbatim including future values.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string? FeeTypeOverride { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonConverter(typeof(FeeMultiplierConverter))]
    public required decimal? FeeMultiplierOverride { get; init; }
    [JsonConverter(typeof(FeeTimestampConverter))]
    public required DateTimeOffset ScheduledTs { get; init; }
    void IJsonOnDeserialized.OnDeserialized()
    {
        if (Id is null || EventTicker is null || SeriesTicker is null) throw new JsonException("Invalid event fee change identity.");
    }
}
