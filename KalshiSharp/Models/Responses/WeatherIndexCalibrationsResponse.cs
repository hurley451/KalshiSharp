using System.Text.Json;
using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Weather-index calibration timeline for a configured city.
/// </summary>
public sealed record WeatherIndexCalibrationsResponse
{
    /// <summary>Index city ID.</summary>
    public required string City { get; init; }

    /// <summary>Temperature units for calibration quantities. Current value is celsius.</summary>
    public required string Units { get; init; }

    /// <summary>Configuration records ordered by effective time.</summary>
    public required IReadOnlyList<WeatherIndexCalibration> Calibrations { get; init; }

    /// <summary>Additional response fields returned by Kalshi.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

/// <summary>
/// One weather-index calibration record.
/// </summary>
public sealed record WeatherIndexCalibration
{
    /// <summary>Configuration version applied by this calibration.</summary>
    public required string ConfigVersion { get; init; }

    /// <summary>Effective time in Unix milliseconds.</summary>
    public required long EffectiveAtMs { get; init; }

    /// <summary>City reference temperature in Celsius.</summary>
    public required decimal CityReferenceC { get; init; }

    /// <summary>Station weights and offsets used by this configuration.</summary>
    public required IReadOnlyList<WeatherIndexCalibrationStation> Stations { get; init; }

    /// <summary>Publication time in Unix milliseconds.</summary>
    public required long PublishedAtMs { get; init; }

    /// <summary>Reason for this calibration or methodology update.</summary>
    public string? ChangeReason { get; init; }

    /// <summary>Calibration-window start in Unix milliseconds.</summary>
    public long? CalibrationWindowStartMs { get; init; }

    /// <summary>Calibration-window end in Unix milliseconds.</summary>
    public long? CalibrationWindowEndMs { get; init; }

    /// <summary>Additional calibration fields returned by Kalshi.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

/// <summary>
/// Station contribution in a weather-index calibration record.
/// </summary>
public sealed record WeatherIndexCalibrationStation
{
    /// <summary>Station identifier.</summary>
    public required string StationId { get; init; }

    /// <summary>Station weight.</summary>
    public required decimal Weight { get; init; }

    /// <summary>Station offset in Celsius.</summary>
    public required decimal OffsetC { get; init; }

    /// <summary>Disposition or update note for this station.</summary>
    public string? UpdateNote { get; init; }

    /// <summary>Additional station fields returned by Kalshi.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
