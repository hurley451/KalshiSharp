using System.Text.Json;
using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Weather-index response for a configured city.
/// </summary>
public sealed record WeatherIndexResponse
{
    /// <summary>Index city ID.</summary>
    public required string City { get; init; }

    /// <summary>Temperature units for the published index. Current value is fahrenheit.</summary>
    public required string Units { get; init; }

    /// <summary>Canonical minute-resolution index points.</summary>
    public required IReadOnlyList<WeatherIndexPoint> Timeseries { get; init; }

    /// <summary>Configuration version of the newest returned point, or empty when no points matched.</summary>
    public required string ConfigVersion { get; init; }

    /// <summary>Additional response fields returned by Kalshi.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

/// <summary>
/// One weather-index point.
/// </summary>
public sealed record WeatherIndexPoint
{
    /// <summary>Point timestamp in Unix milliseconds.</summary>
    public required long T { get; init; }

    /// <summary>Point status.</summary>
    public required string Status { get; init; }

    /// <summary>Index value in Fahrenheit. Absent for incomplete points.</summary>
    public decimal? V { get; init; }

    /// <summary>Number of contributing observations.</summary>
    public int? Contributors { get; init; }

    /// <summary>Receipt-deadline basis for backfilled points. Absent on canonical points.</summary>
    public string? ReceiptBasis { get; init; }

    /// <summary>Per-station readings included when detailed output is requested.</summary>
    public IReadOnlyList<WeatherIndexStation>? Stations { get; init; }

    /// <summary>Additional point fields returned by Kalshi.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

/// <summary>
/// Per-station weather reading.
/// </summary>
public sealed record WeatherIndexStation
{
    /// <summary>Station identifier.</summary>
    public string? StationId { get; init; }

    /// <summary>Station code or pending marker.</summary>
    public string? Code { get; init; }

    /// <summary>Observation source.</summary>
    public string? Source { get; init; }

    /// <summary>Station temperature reading in Fahrenheit.</summary>
    public decimal? TempF { get; init; }

    /// <summary>Observation time in Unix milliseconds.</summary>
    public long? ObsTimeMs { get; init; }

    /// <summary>Receipt time in Unix milliseconds.</summary>
    public long? ReceivedAtMs { get; init; }

    /// <summary>Primary station code when supplied.</summary>
    public string? PrimaryCode { get; init; }

    /// <summary>Additional station fields returned by Kalshi.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
