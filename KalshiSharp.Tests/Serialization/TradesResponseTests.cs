using System.Text.Json;
using KalshiSharp.Models.Responses;
using KalshiSharp.Serialization;
using Xunit;

namespace KalshiSharp.Tests.Serialization;

public sealed class TradesResponseTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"cursor\":\"next\"}")]
    [InlineData("{\"trades\":null}")]
    public void MalformedCollectionThrows(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TradesResponse>(json, KalshiJsonOptions.Default));
    }

    [Fact]
    public void ExplicitEmptyCollectionPreservesCursor()
    {
        var response = JsonSerializer.Deserialize<TradesResponse>("{\"trades\":[],\"cursor\":\"opaque +/=\"}", KalshiJsonOptions.Default)!;
        Assert.Empty(response.Trades);
        Assert.Same(response.Trades, response.Items);
        Assert.Equal("opaque +/=", response.Cursor);
    }

    [Fact]
    public void PopulatedCollectionPreservesExactValues()
    {
        var response = JsonSerializer.Deserialize<TradesResponse>("""
            {"trades":[{"trade_id":"T","ticker":"M","created_time":"2026-10-17T12:00:00Z",
            "count_fp":"1.234567890123456789","yes_price_dollars":"0.123456789012345678"}]}
            """, KalshiJsonOptions.Default)!;
        var trade = Assert.Single(response.Trades);
        Assert.Equal("1.234567890123456789", trade.CountFp);
        Assert.Equal("0.123456789012345678", trade.YesPriceDollars);
    }

    [Fact]
    public void ProgrammaticEmptyResponseRemainsSourceCompatible()
    {
        var response = new TradesResponse();
        var json = JsonSerializer.Serialize(response, KalshiJsonOptions.Default);
        Assert.Empty(JsonSerializer.Deserialize<TradesResponse>(json, KalshiJsonOptions.Default)!.Trades);
    }
}
