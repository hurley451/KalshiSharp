using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KalshiSharp.Core.Serialization.Converters;

/// <summary>
/// JSON converter that handles Unix timestamps in milliseconds from the Kalshi API.
/// Converts to/from <see cref="DateTimeOffset"/>.
/// </summary>
public sealed class UnixTimestampConverter : JsonConverter<DateTimeOffset>
{
    /// <inheritdoc/>
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64()),
            JsonTokenType.String when long.TryParse(reader.GetString(), out var ms) =>
                DateTimeOffset.FromUnixTimeMilliseconds(ms),
            JsonTokenType.String => DateTimeOffset.Parse(reader.GetString()!, CultureInfo.InvariantCulture),
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to DateTimeOffset")
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToUnixTimeMilliseconds());
    }
}

/// <summary>
/// JSON converter that handles nullable Unix timestamps in milliseconds.
/// </summary>
public sealed class NullableUnixTimestampConverter : JsonConverter<DateTimeOffset?>
{
    /// <inheritdoc/>
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetInt64(out var ms) =>
                DateTimeOffset.FromUnixTimeMilliseconds(ms),
            JsonTokenType.String when string.IsNullOrEmpty(reader.GetString()) => null,
            JsonTokenType.String when long.TryParse(reader.GetString(), out var ms) =>
                DateTimeOffset.FromUnixTimeMilliseconds(ms),
            JsonTokenType.String => DateTimeOffset.Parse(reader.GetString()!, CultureInfo.InvariantCulture),
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to DateTimeOffset?")
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value.ToUnixTimeMilliseconds());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
