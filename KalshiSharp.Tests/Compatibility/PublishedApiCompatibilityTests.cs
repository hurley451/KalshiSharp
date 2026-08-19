using System.Text;
using System.Text.Json;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using KalshiSharp.Models.WebSocket;
using KalshiSharp.Serialization;
using Xunit;

namespace KalshiSharp.Tests.Compatibility;

public sealed class PublishedApiCompatibilityTests
{
    [Fact]
    public void CanonicalRequestBuilder_PreservesPublishedFormat()
    {
        var result = CanonicalRequestBuilder.Build(
            1704067200000,
            "get",
            "/trade-api/v2/markets?limit=10",
            ReadOnlySpan<byte>.Empty);

        Encoding.UTF8.GetString(result).Should()
            .Be("1704067200000\nGET\n/trade-api/v2/markets?limit=10\n");
    }

#pragma warning disable CS0618 // Deliberately verify the published legacy compatibility signer.
    [Fact]
    public void HmacSigner_PreservesPublishedGoldenVector()
    {
        using var signer = new HmacSha256RequestSigner("test-api-key", "test-secret-key");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.kalshi.com/trade-api/v2/exchange/status");

        signer.Sign(
            request,
            ReadOnlySpan<byte>.Empty,
            DateTimeOffset.FromUnixTimeMilliseconds(1704067200000));

        request.Headers.GetValues(HmacSha256RequestSigner.AccessSignatureHeader)
            .Should().ContainSingle().Which
            .Should().Be("IGeDgqtsFwD/eG58ZzylEmsVa/PMK+C4cksICcq7VeQ=");
    }
#pragma warning restore CS0618

    [Fact]
    public void EventQuery_SupportsPublishedAndCurrentFilters()
    {
        var legacy = new EventQuery
        {
            Status = MarketStatus.Active,
            WithNestedMarkets = "true"
        };
        var current = new EventQuery
        {
            EventStatus = EventStatus.Open,
            IncludeNestedMarkets = true
        };

        legacy.ToQueryString().Should().Be("?status=active&with_nested_markets=true");
        current.ToQueryString().Should().Be("?status=open&with_nested_markets=true");
    }

    [Fact]
    public void EventResponse_CurrentWireShape_PopulatesPublishedAliases()
    {
        const string json = """
            {
              "event_ticker": "EVENT-1",
              "title": "Event",
              "sub_title": "Subtitle",
              "category": "test",
              "mutually_exclusive": true,
              "collateral_return_type": "binary"
            }
            """;

        var response = JsonSerializer.Deserialize<EventResponse>(json, KalshiJsonOptions.Default)!;

        response.Subtitle.Should().Be("Subtitle");
        response.SubTitle.Should().Be("Subtitle");
        response.MutuallyExclusive.Should().Be("true");
        response.IsMutuallyExclusive.Should().BeTrue();
    }

    [Fact]
    public void PublishedWebSocketMembers_RemainUsable()
    {
        WebSocketMessage heartbeat = new HeartbeatMessage
        {
            Id = 42,
            Sequence = 7,
            Timestamp = 1704067200000
        };
        var update = new OrderBookUpdate
        {
            MarketTicker = "MARKET-1",
            Price = 50,
            Delta = -2,
            Side = "yes",
            Message = new OrderBookUpdate.MessageBody
            {
                MarketTicker = "MARKET-1",
                MarketId = "market-id",
                Side = "yes"
            }
        };
        var trade = new TradeUpdate
        {
            TradeId = "trade-1",
            MarketTicker = "MARKET-1",
            Side = OrderSide.Yes,
            YesPrice = 50,
            NoPrice = 50,
            Count = 1,
            CreatedTimeMs = 1704067200000,
            Message = new TradeUpdate.MessageBody
            {
                TradeId = "trade-1",
                MarketTicker = "MARKET-1"
            }
        };

        heartbeat.TimestampUtc.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1704067200000));
        update.IsYesSide.Should().BeTrue();
        trade.CreatedTime.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1704067200000));
    }
}
