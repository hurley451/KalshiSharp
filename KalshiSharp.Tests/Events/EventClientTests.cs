using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Tests.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Errors;
using KalshiSharp.Http;
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Requests;
using KalshiSharp.Rest.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Events;

/// <summary>
/// HTTP contract tests for the Event client.
/// </summary>
public sealed class EventClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly EventClient _client;
    private readonly IKalshiRequestSigner _signer;

    public EventClientTests()
    {
        _server = WireMockServer.Start();

        var options = Options.Create(new KalshiClientOptions
        {
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret",
            BaseUri = new Uri(_server.Url!),
            Timeout = TimeSpan.FromSeconds(5)
        });

        _signer = new MockRequestSigner(options.Value.ApiKey, options.Value.ApiSecret);
        var clock = new SystemClock();

        var signingHandler = new SigningDelegatingHandler(
            _signer,
            clock,
            NullLogger<SigningDelegatingHandler>.Instance)
        {
            InnerHandler = new HttpClientHandler()
        };

        var httpClient = new HttpClient(signingHandler);
        var kalshiHttpClient = new KalshiHttpClient(
            httpClient,
            options,
            NullLogger<KalshiHttpClient>.Instance);

        _client = new EventClient(kalshiHttpClient);
    }

    public void Dispose()
    {
        _server.Dispose();
        (_signer as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task GetEventAsync_ReturnsEvent()
    {
        // Arrange
        const string eventTicker = "AAPL-EVENT";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/events/{eventTicker}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "event_ticker": "AAPL-EVENT",
                    "title": "Apple Stock Events",
                    "category": "tech",
                    "market_count": 5,
                    "series_ticker": "TECH-SERIES"
                }
                """));

        // Act
        var result = await _client.GetEventAsync(eventTicker);

        // Assert
        result.Should().NotBeNull();
        result.EventTicker.Should().Be("AAPL-EVENT");
        result.Title.Should().Be("Apple Stock Events");
        result.Category.Should().Be("tech");
        result.MarketCount.Should().Be(5);
        result.SeriesTicker.Should().Be("TECH-SERIES");
    }

    [Fact]
    public async Task GetEventAsync_WithNestedMarkets_IncludesMarkets()
    {
        // Arrange
        const string eventTicker = "AAPL-EVENT";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/events/{eventTicker}")
                .WithParam("with_nested_markets", "true")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "event_ticker": "AAPL-EVENT",
                    "title": "Apple Stock Events",
                    "category": "tech",
                    "market_count": 1,
                    "markets": [
                        {
                            "ticker": "AAPL-MARKET-1",
                            "event_ticker": "AAPL-EVENT",
                            "title": "Will Apple reach $200?",
                            "status": "active",
                            "yes_bid": 55,
                            "yes_ask": 57,
                            "no_bid": 43,
                            "no_ask": 45,
                            "volume": 10000,
                            "volume24_h": 500,
                            "open_interest": 2500,
                            "can_close_early": true
                        }
                    ]
                }
                """));

        // Act
        var result = await _client.GetEventAsync(eventTicker, withNestedMarkets: true);

        // Assert
        result.Should().NotBeNull();
        result.EventTicker.Should().Be("AAPL-EVENT");
        result.Markets.Should().NotBeNull();
        result.Markets.Should().HaveCount(1);
        result.Markets![0].Ticker.Should().Be("AAPL-MARKET-1");
        result.Markets[0].Status.Should().Be(MarketStatus.Active);
    }

    [Fact]
    public async Task GetEventAsync_WithNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/events/INVALID-EVENT")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"not_found","message":"Event not found"}"""));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiNotFoundException>(
            () => _client.GetEventAsync("INVALID-EVENT"));

        exception.ErrorCode.Should().Be("not_found");
    }

    [Fact]
    public async Task GetEventAsync_WithEmptyTicker_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetEventAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetEventAsync("   "));
    }

    [Fact]
    public async Task ListEventsAsync_WithNoParameters_ReturnsEvents()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/events")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "items": [
                        {
                            "event_ticker": "EVENT-1",
                            "title": "Event 1",
                            "category": "politics",
                            "market_count": 3
                        },
                        {
                            "event_ticker": "EVENT-2",
                            "title": "Event 2",
                            "category": "economics",
                            "market_count": 2
                        }
                    ],
                    "cursor": "next-page-cursor"
                }
                """));

        // Act
        var result = await _client.ListEventsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items[0].EventTicker.Should().Be("EVENT-1");
        result.Items[0].Category.Should().Be("politics");
        result.Items[1].EventTicker.Should().Be("EVENT-2");
        result.Items[1].Category.Should().Be("economics");
        result.Cursor.Should().Be("next-page-cursor");
        result.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task ListEventsAsync_WithQuery_AppliesFilters()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/events")
                .WithParam("status", "open")
                .WithParam("series_ticker", "SERIES-123")
                .WithParam("limit", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "items": [],
                    "cursor": null
                }
                """));

        var query = new EventQuery
        {
            Status = MarketStatus.Active,
            SeriesTicker = "SERIES-123",
            Limit = 50
        };

        // Act
        var result = await _client.ListEventsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ListEventsAsync_WithCursor_FetchesNextPage()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/events")
                .WithParam("cursor", "page-2-cursor")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "items": [
                        {
                            "event_ticker": "EVENT-3",
                            "title": "Event 3",
                            "category": "sports",
                            "market_count": 1
                        }
                    ],
                    "cursor": null
                }
                """));

        var query = new EventQuery { Cursor = "page-2-cursor" };

        // Act
        var result = await _client.ListEventsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].EventTicker.Should().Be("EVENT-3");
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ListEventsAsync_WithNestedMarkets_AppliesParameter()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/events")
                .WithParam("with_nested_markets", "true")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "items": [
                        {
                            "event_ticker": "EVENT-1",
                            "title": "Event 1",
                            "category": "politics",
                            "market_count": 1,
                            "markets": [
                                {
                                    "ticker": "MARKET-1",
                                    "event_ticker": "EVENT-1",
                                    "title": "Market 1",
                                    "status": "active",
                                    "yes_bid": 50,
                                    "yes_ask": 52,
                                    "no_bid": 48,
                                    "no_ask": 50,
                                    "volume": 1000,
                                    "volume24_h": 100,
                                    "open_interest": 500,
                                    "can_close_early": false
                                }
                            ]
                        }
                    ],
                    "cursor": null
                }
                """));

        var query = new EventQuery { WithNestedMarkets = "true" };

        // Act
        var result = await _client.ListEventsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].Markets.Should().NotBeNull();
        result.Items[0].Markets.Should().HaveCount(1);
        result.Items[0].Markets![0].Ticker.Should().Be("MARKET-1");
    }

    [Fact]
    public async Task GetEventAsync_WithSpecialCharactersInTicker_EncodesCorrectly()
    {
        // Arrange
        const string eventTicker = "EVENT-TEST-2024";

        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/events/{eventTicker}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "event_ticker": "EVENT-TEST-2024",
                    "title": "Test Event",
                    "category": "test",
                    "market_count": 0
                }
                """));

        // Act
        var result = await _client.GetEventAsync(eventTicker);

        // Assert
        result.Should().NotBeNull();
        result.EventTicker.Should().Be(eventTicker);
    }
}
