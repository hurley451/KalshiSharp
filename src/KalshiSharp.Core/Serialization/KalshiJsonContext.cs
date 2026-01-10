using System.Text.Json;
using System.Text.Json.Serialization;
using KalshiSharp.Core.Errors;
using KalshiSharp.Core.Serialization.Converters;
using KalshiSharp.Models.Common;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Core.Serialization;

/// <summary>
/// Source-generated JSON serialization context for KalshiSharp.
/// Provides AOT-compatible and high-performance serialization.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
// Core error type
[JsonSerializable(typeof(KalshiErrorResponse))]
// Response types
[JsonSerializable(typeof(ExchangeStatusResponse))]
[JsonSerializable(typeof(ExchangeScheduleResponse))]
[JsonSerializable(typeof(ScheduleEntry))]
[JsonSerializable(typeof(MarketResponse))]
[JsonSerializable(typeof(OrderResponse))]
[JsonSerializable(typeof(OrderBookResponse))]
[JsonSerializable(typeof(TradeResponse))]
[JsonSerializable(typeof(EventResponse))]
[JsonSerializable(typeof(BalanceResponse))]
[JsonSerializable(typeof(PositionResponse))]
[JsonSerializable(typeof(FillResponse))]
[JsonSerializable(typeof(UserResponse))]
// Paged response types
[JsonSerializable(typeof(PagedResponse<MarketResponse>))]
[JsonSerializable(typeof(PagedResponse<OrderResponse>))]
[JsonSerializable(typeof(PagedResponse<TradeResponse>))]
[JsonSerializable(typeof(PagedResponse<EventResponse>))]
[JsonSerializable(typeof(PagedResponse<PositionResponse>))]
[JsonSerializable(typeof(PagedResponse<FillResponse>))]
// Collection types for responses
[JsonSerializable(typeof(IReadOnlyList<MarketResponse>))]
[JsonSerializable(typeof(IReadOnlyList<OrderResponse>))]
[JsonSerializable(typeof(IReadOnlyList<TradeResponse>))]
[JsonSerializable(typeof(IReadOnlyList<EventResponse>))]
[JsonSerializable(typeof(IReadOnlyList<PositionResponse>))]
[JsonSerializable(typeof(IReadOnlyList<FillResponse>))]
[JsonSerializable(typeof(IReadOnlyList<ScheduleEntry>))]
[JsonSerializable(typeof(IReadOnlyList<int[]>))]
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
