using System.Text.Json;
using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

#pragma warning disable CA1708 // Preserve the published Subtitle member and current SubTitle alias.

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
    [JsonPropertyName("sub_title")]
    public string? Subtitle { get; init; }

    /// <summary>Current spelling of <see cref="Subtitle"/>.</summary>
    [JsonIgnore]
    public string? SubTitle
    {
        get => Subtitle;
        init => Subtitle = value;
    }

    /// <summary>
    /// Category this event belongs to.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Mutually exclusive status ("true" for mutually exclusive).
    /// </summary>
    [JsonConverter(typeof(BooleanStringJsonConverter))]
    public string? MutuallyExclusive { get; init; }

    /// <summary>Strongly typed mutually exclusive status.</summary>
    [JsonIgnore]
    public bool? IsMutuallyExclusive
    {
        get => bool.TryParse(MutuallyExclusive, out var value) ? value : null;
        init => MutuallyExclusive = value?.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Number of markets in this event when supplied by the API.
    /// </summary>
    public int MarketCount { get; init; }

    /// <summary>
    /// Markets belonging to this event.
    /// </summary>
    public IReadOnlyList<MarketResponse>? Markets { get; init; }

    /// <summary>When this event was created, when supplied by the API.</summary>
    public DateTimeOffset? CreatedTime { get; init; }

    /// <summary>When this event closes, when supplied by the API.</summary>
    public DateTimeOffset? CloseTime { get; init; }

    /// <summary>
    /// Series ticker if this event is part of a series.
    /// </summary>
    public string? SeriesTicker { get; init; }

    /// <summary>
    /// Specifies how collateral is returned when markets settle (e.g., 'binary' for standard yes/no markets).
    /// </summary>
    public required string CollateralReturnType { get; init; }

    /// <summary>
    /// Whether this event is available to trade on brokers. 
    /// </summary>
    public bool AvailableOnBrokers { get; init; }

    /// <summary>
    /// The specific date this event is based on. 
    /// Only filled when the event uses a date strike (mutually exclusive with strike_period).
    /// </summary>
    public DateTimeOffset? StrikeDate { get; init; }

    /// <summary>
    /// The time period this event covers (e.g., 'week', 'month'). 
    /// Only filled when the event uses a period strike (mutually exclusive with strike_date).
    /// </summary>
    public string? StrikePeriod { get; init; }

    /// <summary>Sources used to settle the event.</summary>
    public IReadOnlyList<SettlementSource> SettlementSources { get; init; } = [];

    /// <summary>Additional product metadata for this event.</summary>
    public EventProductMetadata? ProductMetadata { get; init; }

    /// <summary>When this event was last updated.</summary>
    public DateTimeOffset? LastUpdatedTs { get; init; }

    /// <summary>Exchange shard that owns this event.</summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>Event-level fee type override.</summary>
    public string? FeeTypeOverride { get; init; }

    /// <summary>Event-level fee multiplier override.</summary>
    public decimal? FeeMultiplierOverride { get; init; }

    /// <summary>A source used to settle an event.</summary>
    public sealed record SettlementSource
    {
        /// <summary>Source name.</summary>
        public required string Name { get; init; }

        /// <summary>Source URL.</summary>
        public required string Url { get; init; }
    }

    /// <summary>Product-specific event metadata.</summary>
    public sealed record EventProductMetadata
    {
        /// <summary>Publication or occurrence cadence.</summary>
        public string? Cadence { get; init; }
    }
}

internal sealed class BooleanStringJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True => bool.TrueString.ToLowerInvariant(),
            JsonTokenType.False => bool.FalseString.ToLowerInvariant(),
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Null => null,
            _ => throw new JsonException("Expected a Boolean, string, or null value.")
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        if (bool.TryParse(value, out var booleanValue))
        {
            writer.WriteBooleanValue(booleanValue);
            return;
        }

        writer.WriteStringValue(value);
    }
}

#pragma warning restore CA1708
