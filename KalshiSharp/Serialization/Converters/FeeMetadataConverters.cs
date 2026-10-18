using System.Text.Json;
using System.Text.Json.Serialization;

namespace KalshiSharp.Serialization.Converters;

// Fee metadata uses OpenAPI numbers and RFC3339 strings, unlike SDK legacy string-decimals/Unix timestamps.
internal sealed class FeeMultiplierConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var value)
            ? value : throw new JsonException("Fee multiplier must be a representable decimal number.");
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
}

internal sealed class FeeTimestampConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && reader.GetString() is { } text &&
            (text.EndsWith('Z') || (text.Length >= 6 && text[^6] is '+' or '-' && text[^3] == ':')) &&
            reader.TryGetDateTimeOffset(out var value)) return value;
        throw new JsonException("Fee timestamp must be an ISO date-time string with an explicit offset.");
    }
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}
