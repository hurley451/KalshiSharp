using System.Text.Json;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents a Kalshi series.
/// </summary>
public sealed record SeriesResponse
{
    /// <summary>Unique series ticker.</summary>
    public required string Ticker { get; init; }

    /// <summary>Event publication frequency.</summary>
    public required string Frequency { get; init; }

    /// <summary>Human-readable series title.</summary>
    public required string Title { get; init; }

    /// <summary>Primary series category.</summary>
    public required string Category { get; init; }

    /// <summary>
    /// Discovery categories that can match the series category filter.
    /// </summary>
    public IReadOnlyList<string>? Categories { get; init; }

    /// <summary>Search and classification tags.</summary>
    public required IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Sources used to settle events in the series.</summary>
    public required IReadOnlyList<SeriesSettlementSource>? SettlementSources { get; init; }

    /// <summary>URL describing the underlying contract.</summary>
    public required string ContractUrl { get; init; }

    /// <summary>URL containing the contract terms.</summary>
    public required string ContractTermsUrl { get; init; }

    /// <summary>Fee calculation type.</summary>
    public required string FeeType { get; init; }

    /// <summary>Multiplier applied by the fee calculation.</summary>
    public required decimal FeeMultiplier { get; init; }

    /// <summary>Additional restrictions applying to the series.</summary>
    public required IReadOnlyList<string>? AdditionalProhibitions { get; init; }

    /// <summary>Product-specific metadata when requested.</summary>
    public JsonElement? ProductMetadata { get; init; }

    /// <summary>Aggregate series volume as a fixed-point quantity string.</summary>
    public string? VolumeFp { get; init; }

    /// <summary>When the series was last updated.</summary>
    public DateTimeOffset? LastUpdatedTs { get; init; }

    /// <summary>Exchange shard that owns the series.</summary>
    public int? ExchangeIndex { get; init; }
}

/// <summary>
/// A source used to settle events in a series.
/// </summary>
public sealed record SeriesSettlementSource
{
    /// <summary>Source name when supplied by the API.</summary>
    public string? Name { get; init; }

    /// <summary>Source URL when supplied by the API.</summary>
    public string? Url { get; init; }
}
