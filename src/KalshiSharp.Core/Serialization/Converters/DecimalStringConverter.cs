using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KalshiSharp.Core.Serialization.Converters;

/// <summary>
/// JSON converter that handles decimal values returned as strings by the Kalshi API.
/// Supports reading from both string and number JSON types.
/// </summary>
public sealed class DecimalStringConverter : JsonConverter<decimal>
{
    /// <inheritdoc/>
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => decimal.Parse(reader.GetString()!, CultureInfo.InvariantCulture),
            JsonTokenType.Number => reader.GetDecimal(),
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to decimal")
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// JSON converter that handles nullable decimal values returned as strings by the Kalshi API.
/// </summary>
public sealed class NullableDecimalStringConverter : JsonConverter<decimal?>
{
    /// <inheritdoc/>
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String when string.IsNullOrEmpty(reader.GetString()) => null,
            JsonTokenType.String => decimal.Parse(reader.GetString()!, CultureInfo.InvariantCulture),
            JsonTokenType.Number => reader.GetDecimal(),
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to decimal?")
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
