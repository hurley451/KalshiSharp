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
                    "event": {
                        "event_ticker": "AAPL-EVENT",
                        "title": "Apple Stock Events",
                        "sub_title": "Q1 2024",
                        "category": "tech",
                        "mutually_exclusive": true,
                        "series_ticker": "TECH-SERIES",
                        "collateral_return_type": "binary",
                        "available_on_brokers": true,
                        "strike_date": "2024-01-01T00:00:00Z",
                        "strike_period": null
                    }
                }
                """));

        // Act
        var result = await _client.GetEventAsync(eventTicker);

        // Assert
        result.Should().NotBeNull();
        result.EventTicker.Should().Be("AAPL-EVENT");
        result.Title.Should().Be("Apple Stock Events");
        result.SubTitle.Should().Be("Q1 2024");
        result.Category.Should().Be("tech");
        result.MutuallyExclusive.Should().BeTrue();
        result.SeriesTicker.Should().Be("TECH-SERIES");
        result.CollateralReturnType.Should().Be("binary");
        result.AvailableOnBrokers.Should().BeTrue();
        result.StrikeDate.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        result.StrikePeriod.Should().BeNull();
        result.Markets.Should().BeNull();
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
                    "event": {
                        "event_ticker": "AAPL-EVENT",
                        "title": "Apple Stock Events",
                        "category": "tech",
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
                        ],
                        "collateral_return_type": "binary"
                    }
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
                    "events": [
                        {
                            "event_ticker": "EVENT-1",
                            "title": "Event 1",
                            "category": "politics",
                            "collateral_return_type": "binary"
                        },
                        {
                            "event_ticker": "EVENT-2",
                            "title": "Event 2",
                            "category": "economics",
                            "collateral_return_type": "binary"
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
                    "events": [],
                    "cursor": null
                }
                """));

        var query = new EventQuery
        {
            Status = EventStatus.Open,
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
                    "events": [
                        {
                            "event_ticker": "EVENT-3",
                            "title": "Event 3",
                            "category": "sports",
                            "collateral_return_type": "binary"
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
                    "events": [
                        {
                            "event_ticker": "EVENT-1",
                            "title": "Event 1",
                            "category": "politics",
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
                            ],
                            "collateral_return_type": "binary"
                        }
                    ],
                    "cursor": null
                }
                """));

        var query = new EventQuery { WithNestedMarkets = true };

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
                    "event": {
                        "event_ticker": "EVENT-TEST-2024",
                        "title": "Test Event",
                        "category": "test",
                        "collateral_return_type": "binary"
                    }
                }
                """));

        // Act
        var result = await _client.GetEventAsync(eventTicker);

        // Assert
        result.Should().NotBeNull();
        result.EventTicker.Should().Be(eventTicker);
    }

    [Fact]
    public async Task GetEventAsync_WithNestedMarkets_MapsAllMarketFields()
    {
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
                    "event": {
                        "event_ticker": "AAPL-EVENT",
                        "title": "Apple Stock Events",
                        "category": "tech",
                        "collateral_return_type": "binary",
                        "markets": [
                            {
                                "ticker": "AAPL-MARKET-1",
                                "event_ticker": "AAPL-EVENT",
                                "title": "Will Apple reach $200?",
                                "subtitle": "Closes Jan 1",
                                "status": "active",
                                "market_type": "binary",
                                "yes_bid": 55,
                                "yes_ask": 57,
                                "no_bid": 43,
                                "no_ask": 45,
                                "last_price": 54,
                                "previous_yes_ask": 52,
                                "previous_yes_bid": 50,
                                "previous_price": 51,
                                "volume": 10000,
                                "volume_24h": 500,
                                "open_interest": 2500,
                                "liquidity": 1200,
                                "notional_value": 100,
                                "tick_size": 1,
                                "open_time": "2024-01-01T09:00:00Z",
                                "close_time": "2024-01-01T17:00:00Z",
                                "expiration_time": "2024-01-01T17:00:00Z",
                                "expected_expiration_time": "2024-01-01T17:00:00Z",
                                "latest_expiration_time": "2024-01-02T17:00:00Z",
                                "created_time": "2023-12-01T00:00:00Z",
                                "settlement_timer_seconds": 3600,
                                "result": "",
                                "expiration_value": "",
                                "can_close_early": true,
                                "category": "finance",
                                "rules_primary": "Primary rules text",
                                "rules_secondary": "Secondary rules text",
                                "yes_sub_title": "Yes subtitle",
                                "no_sub_title": "No subtitle",
                                "risk_limit_cents": 10000,
                                "strike_value": 200.0,
                                "floor_strike": 190.0,
                                "cap_strike": 210.0
                            }
                        ]
                    }
                }
                """));

        var result = await _client.GetEventAsync(eventTicker, withNestedMarkets: true);

        var market = result.Markets![0];
        market.Ticker.Should().Be("AAPL-MARKET-1");
        market.EventTicker.Should().Be("AAPL-EVENT");
        market.Title.Should().Be("Will Apple reach $200?");
        market.Subtitle.Should().Be("Closes Jan 1");
        market.Status.Should().Be(MarketStatus.Active);
        market.MarketType.Should().Be("binary");
        market.YesBid.Should().Be(55);
        market.YesAsk.Should().Be(57);
        market.NoBid.Should().Be(43);
        market.NoAsk.Should().Be(45);
        market.LastPrice.Should().Be(54);
        market.PreviousYesAsk.Should().Be(52);
        market.PreviousYesBid.Should().Be(50);
        market.PreviousPrice.Should().Be(51);
        market.Volume.Should().Be(10000);
        market.Volume24H.Should().Be(500);
        market.OpenInterest.Should().Be(2500);
        market.Liquidity.Should().Be(1200);
        market.NotionalValue.Should().Be(100);
        market.TickSize.Should().Be(1);
        market.OpenTime.Should().Be(new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero));
        market.CloseTime.Should().Be(new DateTimeOffset(2024, 1, 1, 17, 0, 0, TimeSpan.Zero));
        market.ExpirationTime.Should().Be(new DateTimeOffset(2024, 1, 1, 17, 0, 0, TimeSpan.Zero));
        market.ExpectedExpirationTime.Should().Be(new DateTimeOffset(2024, 1, 1, 17, 0, 0, TimeSpan.Zero));
        market.LatestExpirationTime.Should().Be(new DateTimeOffset(2024, 1, 2, 17, 0, 0, TimeSpan.Zero));
        market.CreatedTime.Should().Be(new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero));
        market.SettlementTimerSeconds.Should().Be(3600);
        market.Result.Should().Be("");
        market.ExpirationValue.Should().Be("");
        market.CanCloseEarly.Should().BeTrue();
        market.Category.Should().Be("finance");
        market.RulesPrimary.Should().Be("Primary rules text");
        market.RulesSecondary.Should().Be("Secondary rules text");
        market.YesSubTitle.Should().Be("Yes subtitle");
        market.NoSubTitle.Should().Be("No subtitle");
        market.RiskLimitCents.Should().Be(10000);
        market.StrikeValue.Should().Be(200.0m);
        market.FloorStrike.Should().Be(190.0m);
        market.CapStrike.Should().Be(210.0m);
    }

    [Fact]
    public async Task ListEventsAsync_WithClosedStatus_AppliesStatusFilter()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/events")
                .WithParam("status", "closed")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "events": [],
                    "cursor": null
                }
                """));

        var query = new EventQuery { Status = EventStatus.Closed };

        var result = await _client.ListEventsAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListEventsAsync_ReturnsAllEventFields()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/events")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "events": [
                        {
                            "event_ticker": "INXD-EVENT",
                            "title": "S&P 500 Daily",
                            "sub_title": "Jan 2024",
                            "category": "finance",
                            "mutually_exclusive": false,
                            "series_ticker": "INXD",
                            "collateral_return_type": "binary",
                            "available_on_brokers": false,
                            "strike_date": null,
                            "strike_period": "week"
                        }
                    ],
                    "cursor": null
                }
                """));

        var result = await _client.ListEventsAsync();

        result.Items.Should().HaveCount(1);

        var ev = result.Items[0];
        ev.EventTicker.Should().Be("INXD-EVENT");
        ev.Title.Should().Be("S&P 500 Daily");
        ev.SubTitle.Should().Be("Jan 2024");
        ev.Category.Should().Be("finance");
        ev.MutuallyExclusive.Should().BeFalse();
        ev.SeriesTicker.Should().Be("INXD");
        ev.CollateralReturnType.Should().Be("binary");
        ev.AvailableOnBrokers.Should().BeFalse();
        ev.StrikeDate.Should().BeNull();
        ev.StrikePeriod.Should().Be("week");
        ev.Markets.Should().BeNull();
    }

    [Fact]
    public async Task GetEventAsync_DefaultResponse_PreservesTopLevelMarketsAndMetadata()
    {
        const string eventTicker = "EVENT-CURRENT";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/events/{eventTicker}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "event": {
                        "event_ticker": "EVENT-CURRENT",
                        "title": "Current event",
                        "category": "economics",
                        "collateral_return_type": "binary",
                        "settlement_sources": [{ "name": "Source", "url": "https://example.com" }],
                        "product_metadata": { "cadence": "daily" },
                        "exchange_index": 1
                    },
                    "markets": [{
                        "ticker": "MARKET-CURRENT",
                        "event_ticker": "EVENT-CURRENT",
                        "title": "Current market",
                        "status": "active",
                        "yes_bid_dollars": "0.5000",
                        "exchange_index": 1
                    }]
                }
                """));

        var result = await _client.GetEventAsync(eventTicker);

        result.Markets.Should().ContainSingle();
        result.Markets![0].YesBidDollars.Should().Be("0.5000");
        result.SettlementSources.Should().ContainSingle().Which.Name.Should().Be("Source");
        result.ProductMetadata!.Cadence.Should().Be("daily");
        result.ExchangeIndex.Should().Be(1);
    }

    [Fact]
    public async Task ListEventsAsync_WithTickerAndCloseFilters_AppliesParameters()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/events")
                .WithParam("tickers", "EVENT-1,EVENT-2")
                .WithParam("min_close_ts", "1755600000")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{ "events": [], "cursor": null }"""));

        var result = await _client.ListEventsAsync(new EventQuery
        {
            Tickers = ["EVENT-1", "EVENT-2"],
            MinCloseTime = DateTimeOffset.FromUnixTimeSeconds(1755600000)
        });

        result.Items.Should().BeEmpty();
    }
}
