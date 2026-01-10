using System.Text.Json;
using System.Text.Json.Serialization;
using KalshiSharp.Core.Errors;
using KalshiSharp.Core.Serialization.Converters;

namespace KalshiSharp.Core.Serialization;

/// <summary>
/// Source-generated JSON serialization context for KalshiSharp.
/// Provides AOT-compatible and high-performance serialization.
/// </summary>
/// <remarks>
/// Additional types from KalshiSharp.Models will be added as [JsonSerializable] attributes
/// in subsequent implementation phases.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(KalshiErrorResponse))]
internal sealed partial class KalshiJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Provides pre-configured <see cref="JsonSerializerOptions"/> for Kalshi API serialization.
/// </summary>
public static class KalshiJsonOptions
{
    private static JsonSerializerOptions? _options;

    /// <summary>
    /// Gets the singleton <see cref="JsonSerializerOptions"/> configured for Kalshi API.
    /// </summary>
    public static JsonSerializerOptions Default => _options ??= CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        options.Converters.Add(new DecimalStringConverter());
        options.Converters.Add(new NullableDecimalStringConverter());
        options.Converters.Add(new UnixTimestampConverter());
        options.Converters.Add(new NullableUnixTimestampConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));

        return options;
    }

    /// <summary>
    /// Gets the source-generated type info resolver for known Kalshi types.
    /// </summary>
    internal static KalshiJsonContext SourceGenContext => KalshiJsonContext.Default;
}
