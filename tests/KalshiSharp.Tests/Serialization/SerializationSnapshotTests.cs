using System.Text.Json;
using FluentAssertions;
using KalshiSharp.Core.Errors;
using KalshiSharp.Core.Serialization;
using KalshiSharp.Core.Serialization.Converters;

namespace KalshiSharp.Tests.Serialization;

public class SerializationSnapshotTests
{
    private readonly JsonSerializerOptions _options = KalshiJsonOptions.Default;

    #region DecimalStringConverter Tests

    [Theory]
    [InlineData("\"123.45\"", 123.45)]
    [InlineData("\"0.99\"", 0.99)]
    [InlineData("\"0\"", 0)]
    [InlineData("\"1000000.123456\"", 1000000.123456)]
    [InlineData("123.45", 123.45)]
    [InlineData("0", 0)]
    public void DecimalStringConverter_Read_VariousFormats(string json, decimal expected)
    {
        var result = JsonSerializer.Deserialize<decimal>(json, _options);
        result.Should().Be(expected);
    }

    [Fact]
    public void DecimalStringConverter_Write_ProducesString()
    {
        var result = JsonSerializer.Serialize(123.45m, _options);
        result.Should().Be("\"123.45\"");
    }

    [Fact]
    public void NullableDecimalStringConverter_Read_Null()
    {
        var result = JsonSerializer.Deserialize<decimal?>("null", _options);
        result.Should().BeNull();
    }

    [Fact]
    public void NullableDecimalStringConverter_Read_EmptyString()
    {
        var result = JsonSerializer.Deserialize<decimal?>("\"\"", _options);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("\"99.99\"", 99.99)]
    [InlineData("42.5", 42.5)]
    public void NullableDecimalStringConverter_Read_ValidValue(string json, decimal expected)
    {
        var result = JsonSerializer.Deserialize<decimal?>(json, _options);
        result.Should().Be(expected);
    }

    [Fact]
    public void DecimalStringConverter_Read_InvalidToken_Throws()
    {
        var act = () => JsonSerializer.Deserialize<decimal>("true", _options);
        act.Should().Throw<JsonException>()
            .WithMessage("*Cannot convert*");
    }

    #endregion

    #region UnixTimestampConverter Tests

    [Fact]
    public void UnixTimestampConverter_Read_FromNumber()
    {
        // 1704067200000 = 2024-01-01T00:00:00Z
        var result = JsonSerializer.Deserialize<DateTimeOffset>("1704067200000", _options);
        result.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1704067200000));
    }

    [Fact]
    public void UnixTimestampConverter_Read_FromStringNumber()
    {
        var result = JsonSerializer.Deserialize<DateTimeOffset>("\"1704067200000\"", _options);
        result.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1704067200000));
    }

    [Fact]
    public void UnixTimestampConverter_Read_FromIsoString()
    {
        var result = JsonSerializer.Deserialize<DateTimeOffset>("\"2024-01-01T00:00:00Z\"", _options);
        result.Year.Should().Be(2024);
        result.Month.Should().Be(1);
        result.Day.Should().Be(1);
    }

    [Fact]
    public void UnixTimestampConverter_Write_ProducesNumber()
    {
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1704067200000);
        var result = JsonSerializer.Serialize(timestamp, _options);
        result.Should().Be("1704067200000");
    }

    [Fact]
    public void NullableUnixTimestampConverter_Read_Null()
    {
        var result = JsonSerializer.Deserialize<DateTimeOffset?>("null", _options);
        result.Should().BeNull();
    }

    [Fact]
    public void NullableUnixTimestampConverter_Read_EmptyString()
    {
        var result = JsonSerializer.Deserialize<DateTimeOffset?>("\"\"", _options);
        result.Should().BeNull();
    }

    [Fact]
    public void NullableUnixTimestampConverter_Read_ValidValue()
    {
        var result = JsonSerializer.Deserialize<DateTimeOffset?>("1704067200000", _options);
        result.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1704067200000));
    }

    [Fact]
    public void UnixTimestampConverter_Read_InvalidToken_Throws()
    {
        var act = () => JsonSerializer.Deserialize<DateTimeOffset>("true", _options);
        act.Should().Throw<JsonException>()
            .WithMessage("*Cannot convert*");
    }

    #endregion

    #region KalshiErrorResponse Tests

    [Fact]
    public void KalshiErrorResponse_Deserialize_FullResponse()
    {
        const string json = """
            {
                "code": "INVALID_REQUEST",
                "message": "The request was invalid",
                "errors": {
                    "price": ["must be between 0 and 100"],
                    "quantity": ["must be positive", "must be integer"]
                }
            }
            """;

        var result = JsonSerializer.Deserialize<KalshiErrorResponse>(json, _options);

        result.Should().NotBeNull();
        result!.Code.Should().Be("INVALID_REQUEST");
        result.Message.Should().Be("The request was invalid");
        result.Errors.Should().ContainKey("price");
        result.Errors!["price"].Should().ContainSingle("must be between 0 and 100");
        result.Errors.Should().ContainKey("quantity");
        result.Errors["quantity"].Should().HaveCount(2);
    }

    [Fact]
    public void KalshiErrorResponse_Deserialize_MinimalResponse()
    {
        const string json = """{"code": "NOT_FOUND"}""";

        var result = JsonSerializer.Deserialize<KalshiErrorResponse>(json, _options);

        result.Should().NotBeNull();
        result!.Code.Should().Be("NOT_FOUND");
        result.Message.Should().BeNull();
        result.Errors.Should().BeNull();
    }

    [Fact]
    public void KalshiErrorResponse_Serialize_OmitsNulls()
    {
        var error = new KalshiErrorResponse { Code = "TEST_ERROR" };
        var result = JsonSerializer.Serialize(error, _options);

        result.Should().NotContain("message");
        result.Should().NotContain("errors");
        result.Should().Contain("code");
    }

    #endregion

    #region KalshiJsonOptions Tests

    [Fact]
    public void KalshiJsonOptions_Default_ReturnsConsistentInstance()
    {
        var options1 = KalshiJsonOptions.Default;
        var options2 = KalshiJsonOptions.Default;
        options1.Should().BeSameAs(options2);
    }

    [Fact]
    public void KalshiJsonOptions_Default_UsesSnakeCaseNaming()
    {
        var options = KalshiJsonOptions.Default;
        options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.SnakeCaseLower);
    }

    [Fact]
    public void KalshiJsonOptions_Default_IgnoresNullsWhenWriting()
    {
        var options = KalshiJsonOptions.Default;
        options.DefaultIgnoreCondition.Should().Be(System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);
    }

    [Fact]
    public void KalshiJsonOptions_Default_HasRequiredConverters()
    {
        var options = KalshiJsonOptions.Default;

        options.Converters.Should().Contain(c => c is DecimalStringConverter);
        options.Converters.Should().Contain(c => c is NullableDecimalStringConverter);
        options.Converters.Should().Contain(c => c is UnixTimestampConverter);
        options.Converters.Should().Contain(c => c is NullableUnixTimestampConverter);
    }

    #endregion

    #region Complex Object Serialization Tests

    public record TestOrderDto
    {
        public string? OrderId { get; init; }
        public decimal Price { get; init; }
        public decimal? StopPrice { get; init; }
        public DateTimeOffset CreatedTime { get; init; }
        public DateTimeOffset? ExpirationTime { get; init; }
    }

    [Fact]
    public void ComplexObject_RoundTrip_PreservesData()
    {
        var original = new TestOrderDto
        {
            OrderId = "order-123",
            Price = 0.55m,
            StopPrice = null,
            CreatedTime = DateTimeOffset.FromUnixTimeMilliseconds(1704067200000),
            ExpirationTime = DateTimeOffset.FromUnixTimeMilliseconds(1704153600000)
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<TestOrderDto>(json, _options);

        deserialized.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void ComplexObject_Serialize_UsesSnakeCase()
    {
        var dto = new TestOrderDto
        {
            OrderId = "123",
            Price = 0.50m,
            CreatedTime = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(dto, _options);

        json.Should().Contain("order_id");
        json.Should().Contain("created_time");
        json.Should().NotContain("OrderId");
        json.Should().NotContain("CreatedTime");
    }

    [Fact]
    public void ComplexObject_Deserialize_FromSnakeCase()
    {
        const string json = """
            {
                "order_id": "order-456",
                "price": "0.75",
                "created_time": 1704067200000
            }
            """;

        var result = JsonSerializer.Deserialize<TestOrderDto>(json, _options);

        result.Should().NotBeNull();
        result!.OrderId.Should().Be("order-456");
        result.Price.Should().Be(0.75m);
        result.CreatedTime.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1704067200000));
    }

    #endregion
}
