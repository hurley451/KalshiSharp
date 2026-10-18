using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KalshiSharp.Serialization.Converters;

// Fee metadata uses OpenAPI numbers and RFC3339 strings, unlike SDK legacy string-decimals/Unix timestamps.
internal sealed class FeeMultiplierConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number) throw InvalidMultiplier();
        using var document = JsonDocument.ParseValue(ref reader);
        var raw = document.RootElement.GetRawText();
        var negative = raw[0] == '-';
        var exponentIndex = raw.IndexOfAny(['e', 'E']);
        var mantissa = raw[(negative ? 1 : 0)..(exponentIndex < 0 ? raw.Length : exponentIndex)];
        var point = mantissa.IndexOf('.');
        var fractionalDigits = point < 0 ? 0 : mantissa.Length - point - 1;
        var digits = mantissa.Replace(".", "", StringComparison.Ordinal).TrimStart('0');
        if (digits.Length == 0) return decimal.Zero;

        // Normalize the exact base-ten coefficient before any decimal conversion.
        // Decimal parsing of a fractional token can silently round or underflow.
        var significant = digits.TrimEnd('0');
        var exponent = exponentIndex < 0 ? BigInteger.Zero : BigInteger.Parse(raw.AsSpan(exponentIndex + 1),
            NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        var power = exponent - fractionalDigits + digits.Length - significant.Length;
        if (power < -28 || power > 28) throw InvalidMultiplier();
        if (power > 0) significant += new string('0', (int)power);
        if (!decimal.TryParse(significant, NumberStyles.None, CultureInfo.InvariantCulture, out var coefficient))
            throw InvalidMultiplier();
        var bits = decimal.GetBits(coefficient);
        return new decimal(bits[0], bits[1], bits[2], negative, power < 0 ? (byte)(-power) : (byte)0);
    }
    private static JsonException InvalidMultiplier() => new("Fee multiplier must be an exactly representable decimal number.");
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
