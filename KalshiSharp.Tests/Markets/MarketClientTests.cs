using System.Globalization;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Tests.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Errors;
using KalshiSharp.Http;
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Requests;
using KalshiSharp.Rest.Markets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Markets;

/// <summary>
/// HTTP contract tests for the Market client.
/// </summary>
public sealed class MarketClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly MarketClient _client;
    private readonly IKalshiRequestSigner _signer;

    public MarketClientTests()
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

        _client = new MarketClient(kalshiHttpClient);
    }

    public void Dispose()
    {
        _server.Dispose();
        (_signer as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task GetMarketAsync_ReturnsMarket()
    {
        // Arrange
        const string ticker = "AAPL-2024-01-01";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/markets/{ticker}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "market": {
                        "ticker": "AAPL-2024-01-01",
                        "event_ticker": "AAPL-EVENT",
                        "title": "Will Apple reach $200?",
                        "subtitle": "Closes Jan 1",
                        "status": "active",
                        "market_type": "binary",
                        "yes_bid_dollars": "0.55",
                        "yes_bid_size_fp": "100.00",
                        "yes_ask_dollars": "0.57",
                        "yes_ask_size_fp": "200.00",
                        "no_bid_dollars": "0.43",
                        "no_ask_dollars": "0.45",
                        "last_price_dollars": "0.54",
                        "volume_fp": "10000.00",
                        "volume_24h_fp": "500.00",
                        "open_interest_fp": "2500.00",
                        "notional_value_dollars": "1.00",
                        "previous_yes_bid_dollars": "0.50",
                        "previous_yes_ask_dollars": "0.52",
                        "previous_price_dollars": "0.51",
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
                }
                """));

        // Act
        var result = await _client.GetMarketAsync(ticker);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be("AAPL-2024-01-01");
        result.EventTicker.Should().Be("AAPL-EVENT");
        result.Title.Should().Be("Will Apple reach $200?");
        result.Subtitle.Should().Be("Closes Jan 1");
        result.Status.Should().Be(MarketStatus.Active);
        result.MarketType.Should().Be("binary");
        result.YesBidDollars.Should().Be("0.55");
        result.YesBidSizeFp.Should().Be("100.00");
        result.YesAskDollars.Should().Be("0.57");
        result.YesAskSizeFp.Should().Be("200.00");
        result.NoBidDollars.Should().Be("0.43");
        result.NoAskDollars.Should().Be("0.45");
        result.LastPriceDollars.Should().Be("0.54");
        result.VolumeFp.Should().Be("10000.00");
        result.Volume24hFp.Should().Be("500.00");
        result.OpenInterestFp.Should().Be("2500.00");
        result.NotionalValueDollars.Should().Be("1.00");
        result.PreviousYesBidDollars.Should().Be("0.50");
        result.PreviousYesAskDollars.Should().Be("0.52");
        result.PreviousPriceDollars.Should().Be("0.51");
        result.TickSize.Should().Be(1);
        result.OpenTime.Should().Be(new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero));
        result.CloseTime.Should().Be(new DateTimeOffset(2024, 1, 1, 17, 0, 0, TimeSpan.Zero));
        result.ExpirationTime.Should().Be(new DateTimeOffset(2024, 1, 1, 17, 0, 0, TimeSpan.Zero));
        result.ExpectedExpirationTime.Should().Be(new DateTimeOffset(2024, 1, 1, 17, 0, 0, TimeSpan.Zero));
        result.LatestExpirationTime.Should().Be(new DateTimeOffset(2024, 1, 2, 17, 0, 0, TimeSpan.Zero));
        result.CreatedTime.Should().Be(new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero));
        result.SettlementTimerSeconds.Should().Be(3600);
        result.CanCloseEarly.Should().BeTrue();
        result.Category.Should().Be("finance");
        result.RulesPrimary.Should().Be("Primary rules text");
        result.RulesSecondary.Should().Be("Secondary rules text");
        result.YesSubTitle.Should().Be("Yes subtitle");
        result.NoSubTitle.Should().Be("No subtitle");
        result.RiskLimitCents.Should().Be(10000);
        result.StrikeValue.Should().Be(200.0m);
        result.FloorStrike.Should().Be(190.0m);
        result.CapStrike.Should().Be(210.0m);
    }

    [Fact]
    public async Task GetMarketAsync_WithNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/markets/INVALID-TICKER")
                . UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"not_found","message":"Market not found"}"""));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiNotFoundException>(
            () => _client.GetMarketAsync("INVALID-TICKER"));

        exception.ErrorCode.Should().Be("not_found");
    }

    [Fact]
    public async Task GetMarketAsync_WithEmptyTicker_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetMarketAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetMarketAsync("   "));
    }

    [Fact]
    public async Task ListMarketsAsync_WithNoParameters_ReturnsMarkets()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/markets")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "markets": [
                        {
                            "ticker": "MARKET-1",
                            "event_ticker": "EVENT-1",
                            "title": "Market 1",
                            "subtitle": "Subtitle 1",
                            "status": "active",
                            "market_type": "binary",
                            "yes_bid_dollars": "0.50",
                            "yes_bid_size_fp": "50.00",
                            "yes_ask_dollars": "0.52",
                            "yes_ask_size_fp": "75.00",
                            "no_bid_dollars": "0.48",
                            "no_ask_dollars": "0.50",
                            "last_price_dollars": "0.51",
                            "volume_fp": "1000.00",
                            "volume_24h_fp": "100.00",
                            "open_interest_fp": "500.00",
                            "notional_value_dollars": "1.00",
                            "previous_yes_bid_dollars": "0.49",
                            "previous_yes_ask_dollars": "0.51",
                            "previous_price_dollars": "0.50",
                            "tick_size": 1,
                            "open_time": "2024-01-01T09:00:00Z",
                            "close_time": "2024-01-01T17:00:00Z",
                            "can_close_early": false,
                            "category": "politics"
                        }
                    ],
                    "cursor": "next-page-cursor"
                }
                """));

        // Act
        var result = await _client.ListMarketsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Cursor.Should().Be("next-page-cursor");
        result.HasMore.Should().BeTrue();

        var market = result.Items[0];
        market.Ticker.Should().Be("MARKET-1");
        market.EventTicker.Should().Be("EVENT-1");
        market.Title.Should().Be("Market 1");
        market.Subtitle.Should().Be("Subtitle 1");
        market.Status.Should().Be(MarketStatus.Active);
        market.MarketType.Should().Be("binary");
        market.YesBidDollars.Should().Be("0.50");
        market.YesBidSizeFp.Should().Be("50.00");
        market.YesAskDollars.Should().Be("0.52");
        market.YesAskSizeFp.Should().Be("75.00");
        market.NoBidDollars.Should().Be("0.48");
        market.NoAskDollars.Should().Be("0.50");
        market.LastPriceDollars.Should().Be("0.51");
        market.VolumeFp.Should().Be("1000.00");
        market.Volume24hFp.Should().Be("100.00");
        market.OpenInterestFp.Should().Be("500.00");
        market.NotionalValueDollars.Should().Be("1.00");
        market.PreviousYesBidDollars.Should().Be("0.49");
        market.PreviousYesAskDollars.Should().Be("0.51");
        market.PreviousPriceDollars.Should().Be("0.50");
        market.TickSize.Should().Be(1);
        market.OpenTime.Should().Be(new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero));
        market.CloseTime.Should().Be(new DateTimeOffset(2024, 1, 1, 17, 0, 0, TimeSpan.Zero));
        market.CanCloseEarly.Should().BeFalse();
        market.Category.Should().Be("politics");
    }

    [Fact]
    public async Task ListMarketsAsync_WithQuery_AppliesFilters()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/markets")
                .WithParam("status", "open")
                .WithParam("event_ticker", "EVENT-123")
                .WithParam("limit", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "markets": [],
                    "cursor": null
                }
                """));

        var query = new MarketQuery
        {
            Status = MarketStatus.Active,
            EventTicker = "EVENT-123",
            Limit = 50
        };

        // Act
        var result = await _client.ListMarketsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ListMarketsAsync_WithCursor_FetchesNextPage()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/markets")
                .WithParam("cursor", "page-2-cursor")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "markets": [
                        {
                            "ticker": "MARKET-2",
                            "event_ticker": "EVENT-2",
                            "title": "Market 2",
                            "status": "closed",
                            "yes_bid_dollars": "0.00",
                            "yes_ask_dollars": "0.00",
                            "no_bid_dollars": "0.00",
                            "no_ask_dollars": "0.00",
                            "volume_fp": "5000.00",
                            "volume_24h_fp": "0.00",
                            "open_interest_fp": "0.00",
                            "can_close_early": false
                        }
                    ],
                    "cursor": null
                }
                """));

        var query = new MarketQuery { Cursor = "page-2-cursor" };

        // Act
        var result = await _client.ListMarketsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].Ticker.Should().Be("MARKET-2");
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrderBookAsync_ReturnsOrderBook()
    {
        // Arrange
        const string ticker = "MARKET-XYZ";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/markets/{ticker}/orderbook")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "orderbook": {
                        "yes": [[55, 100], [54, 200], [53, 150]],
                        "no": [[45, 100], [44, 250], [43, 175]]
                    }
                }
                """));

        // Act
        var result = await _client.GetOrderBookAsync(ticker);

        // Assert
        result.Should().NotBeNull();
        result.Orderbook.Yes.Should().HaveCount(3);
        result.Orderbook.Yes[0][0].Should().Be(55);
        result.Orderbook.Yes[0][1].Should().Be(100);
        result.Orderbook.No.Should().HaveCount(3);
        result.Orderbook.No[0][0].Should().Be(45);
        result.Orderbook.No[0][1].Should().Be(100);
    }

    [Fact]
    public async Task GetOrderBookAsync_WithDepth_AppliesDepthParameter()
    {
        // Arrange
        const string ticker = "MARKET-XYZ";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/markets/{ticker}/orderbook")
                .WithParam("depth", "5")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "orderbook": {
                        "yes": [[55, 100]],
                        "no": [[45, 100]]
                    }
                }
                """));

        // Act
        var result = await _client.GetOrderBookAsync(ticker, depth: 5);

        // Assert
        result.Should().NotBeNull();
        result.Orderbook.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTradesAsync_ReturnsTrades()
    {
        // Arrange
        const string ticker = "MARKET-ABC";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/markets/{ticker}/trades")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "trades": [
                        {
                            "trade_id": "trade-001",
                            "ticker": "MARKET-ABC",
                            "side": "yes",
                            "yes_price": 55,
                            "no_price": 45,
                            "count": 10,
                            "created_time": "2024-01-01T10:00:00Z",
                            "taker_side": "yes"
                        }
                    ],
                    "cursor": "trades-cursor"
                }
                """));

        // Act
        var result = await _client.GetTradesAsync(ticker);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Cursor.Should().Be("trades-cursor");
        result.HasMore.Should().BeTrue();

        var trade = result.Items[0];
        trade.TradeId.Should().Be("trade-001");
        trade.Ticker.Should().Be("MARKET-ABC");
        trade.Side.Should().Be(OrderSide.Yes);
        trade.YesPrice.Should().Be(55);
        trade.NoPrice.Should().Be(45);
        trade.Count.Should().Be(10);
        trade.CreatedTime.Should().Be(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));
        trade.TakerSide.Should().Be("yes");
    }

    [Fact]
    public async Task GetTradesAsync_WithPagination_AppliesParameters()
    {
        // Arrange
        const string ticker = "MARKET-ABC";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/markets/{ticker}/trades")
                .WithParam("cursor", "page-2")
                .WithParam("limit", "25")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "trades": [],
                    "cursor": null
                }
                """));

        // Act
        var result = await _client.GetTradesAsync(ticker, cursor: "page-2", limit: 25);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task GetTradesAsync_WithEmptyTicker_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetTradesAsync(""));
    }

    [Fact]
    public async Task GetOrderBookAsync_WithEmptyTicker_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetOrderBookAsync(""));
    }

    [Fact]
    public async Task GetMarketAsync_WithSpecialCharactersInTicker_EncodesCorrectly()
    {
        // Arrange
        const string ticker = "MARKET-TEST-2024";

        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/markets/{ticker}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "market": {
                        "ticker": "MARKET-TEST-2024",
                        "event_ticker": "EVENT",
                        "title": "Test",
                        "status": "active",
                        "yes_bid_dollars": "0.50",
                        "yes_ask_dollars": "0.50",
                        "no_bid_dollars": "0.50",
                        "no_ask_dollars": "0.50",
                        "volume_fp": "0.00",
                        "volume_24h_fp": "0.00",
                        "open_interest_fp": "0.00",
                        "can_close_early": false
                    }
                }
                """));

        // Act
        var result = await _client.GetMarketAsync(ticker);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be(ticker);
    }

    [Fact]
    public async Task GetMarketCandlesticks_ReturnsCandlesticks()
    {
        // Arrange
        const string seriesTicker = "INXD";
        const string ticker = "INXD-24JAN01";
        var startTs = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endTs = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);

        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/series/{seriesTicker}/markets/{ticker}/candlesticks")
                .WithParam("start_ts", startTs.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
                .WithParam("end_ts", endTs.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
                .WithParam("period_interval", "60")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "ticker": "INXD-24JAN01",
                    "candlesticks": [
                        {
                            "end_period_ts": 1704067200,
                            "yes_bid": {
                                "open_dollars": "0.50",
                                "low_dollars": "0.48",
                                "high_dollars": "0.55",
                                "close_dollars": "0.53"
                            },
                            "yes_ask": {
                                "open_dollars": "0.52",
                                "low_dollars": "0.50",
                                "high_dollars": "0.57",
                                "close_dollars": "0.55"
                            },
                            "price": {
                                "open_dollars": "0.51",
                                "low_dollars": "0.49",
                                "high_dollars": "0.56",
                                "close_dollars": "0.54",
                                "mean_dollars": "0.52",
                                "previous_dollars": "0.50",
                                "min_dollars": "0.49",
                                "max_dollars": "0.56"
                            },
                            "volume_fp": "250.00",
                            "open_interest_fp": "1000.00"
                        }
                    ]
                }
                """));

        var query = new MarketCandlesticksQuery
        {
            StartTimestamp = startTs,
            EndTimestamp = endTs,
            PeriodInterval = PeriodInterval.OneHour
        };

        // Act
        var result = await _client.GetMarketCandlesticks(seriesTicker, ticker, query);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be("INXD-24JAN01");
        result.Candlesticks.Should().HaveCount(1);

        var candle = result.Candlesticks[0];
        candle.EndPeriodTimestamp.Should().Be(1704067200);
        candle.VolumeFp.Should().Be("250.00");
        candle.OpenInterestFp.Should().Be("1000.00");

        candle.YesBid.OpenDollars.Should().Be("0.50");
        candle.YesBid.LowDollars.Should().Be("0.48");
        candle.YesBid.HighDollars.Should().Be("0.55");
        candle.YesBid.CloseDollars.Should().Be("0.53");

        candle.YesAsk.OpenDollars.Should().Be("0.52");
        candle.YesAsk.LowDollars.Should().Be("0.50");
        candle.YesAsk.HighDollars.Should().Be("0.57");
        candle.YesAsk.CloseDollars.Should().Be("0.55");

        candle.Price.OpenDollars.Should().Be("0.51");
        candle.Price.LowDollars.Should().Be("0.49");
        candle.Price.HighDollars.Should().Be("0.56");
        candle.Price.CloseDollars.Should().Be("0.54");
        candle.Price.MeanDollars.Should().Be("0.52");
        candle.Price.PreviousDollars.Should().Be("0.50");
        candle.Price.MinDollars.Should().Be("0.49");
        candle.Price.MaxDollars.Should().Be("0.56");
    }

    [Fact]
    public async Task GetMarketCandlesticks_WithIncludeLatestBeforeStart_AppliesParameter()
    {
        // Arrange
        const string seriesTicker = "INXD";
        const string ticker = "INXD-24JAN01";
        var startTs = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endTs = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);

        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/series/{seriesTicker}/markets/{ticker}/candlesticks")
                .WithParam("include_latest_before_start", "true")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "ticker": "INXD-24JAN01",
                    "candlesticks": []
                }
                """));

        var query = new MarketCandlesticksQuery
        {
            StartTimestamp = startTs,
            EndTimestamp = endTs,
            PeriodInterval = PeriodInterval.OneHour,
            IncludeLatestBeforeStart = true
        };

        // Act
        var result = await _client.GetMarketCandlesticks(seriesTicker, ticker, query);

        // Assert
        result.Should().NotBeNull();
        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMarketCandlesticks_WithNoPriceTrades_PriceFieldsAreNull()
    {
        // Arrange
        const string seriesTicker = "INXD";
        const string ticker = "INXD-24JAN01";
        var startTs = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endTs = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);

        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/series/{seriesTicker}/markets/{ticker}/candlesticks")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "ticker": "INXD-24JAN01",
                    "candlesticks": [
                        {
                            "end_period_ts": 1704067200,
                            "yes_bid": {
                                "open_dollars": "0.50",
                                "low_dollars": "0.50",
                                "high_dollars": "0.50",
                                "close_dollars": "0.50"
                            },
                            "yes_ask": {
                                "open_dollars": "0.52",
                                "low_dollars": "0.52",
                                "high_dollars": "0.52",
                                "close_dollars": "0.52"
                            },
                            "price": {
                                "open_dollars": null,
                                "low_dollars": null,
                                "high_dollars": null,
                                "close_dollars": null,
                                "mean_dollars": null,
                                "previous_dollars": "0.50",
                                "min_dollars": null,
                                "max_dollars": null
                            },
                            "volume_fp": "0.00",
                            "open_interest_fp": "500.00"
                        }
                    ]
                }
                """));

        var query = new MarketCandlesticksQuery
        {
            StartTimestamp = startTs,
            EndTimestamp = endTs,
            PeriodInterval = PeriodInterval.OneHour
        };

        // Act
        var result = await _client.GetMarketCandlesticks(seriesTicker, ticker, query);

        // Assert
        var candle = result.Candlesticks[0];
        candle.Price.OpenDollars.Should().BeNull();
        candle.Price.LowDollars.Should().BeNull();
        candle.Price.HighDollars.Should().BeNull();
        candle.Price.CloseDollars.Should().BeNull();
        candle.Price.MeanDollars.Should().BeNull();
        candle.Price.PreviousDollars.Should().Be("0.50");
        candle.Price.MinDollars.Should().BeNull();
        candle.Price.MaxDollars.Should().BeNull();
        candle.VolumeFp.Should().Be("0.00");
    }

    [Fact]
    public async Task GetMarketCandlesticks_WithEmptySeriesTicker_ThrowsArgumentException()
    {
        // Arrange
        var query = new MarketCandlesticksQuery
        {
            StartTimestamp = DateTimeOffset.UtcNow.AddDays(-1),
            EndTimestamp = DateTimeOffset.UtcNow,
            PeriodInterval = PeriodInterval.OneHour
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetMarketCandlesticks("", "INXD-24JAN01", query));
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetMarketCandlesticks("   ", "INXD-24JAN01", query));
    }

    [Fact]
    public async Task GetMarketCandlesticks_WithEmptyTicker_ThrowsArgumentException()
    {
        // Arrange
        var query = new MarketCandlesticksQuery
        {
            StartTimestamp = DateTimeOffset.UtcNow.AddDays(-1),
            EndTimestamp = DateTimeOffset.UtcNow,
            PeriodInterval = PeriodInterval.OneHour
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetMarketCandlesticks("INXD", "", query));
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetMarketCandlesticks("INXD", "   ", query));
    }

    [Fact]
    public async Task GetBatchMarketCandlesticksAsync_ReturnsCandlesticksPerMarket()
    {
        // Arrange
        var startTs = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endTs = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);

        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/markets/candlesticks")
                .WithParam("market_tickers", "MARKET-A,MARKET-B")
                .WithParam("period_interval", "1440")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "markets": [
                        {
                            "market_ticker": "MARKET-A",
                            "candlesticks": [
                                {
                                    "end_period_ts": 1704067200,
                                    "yes_bid": {
                                        "open_dollars": "0.40",
                                        "low_dollars": "0.38",
                                        "high_dollars": "0.45",
                                        "close_dollars": "0.43"
                                    },
                                    "yes_ask": {
                                        "open_dollars": "0.42",
                                        "low_dollars": "0.40",
                                        "high_dollars": "0.47",
                                        "close_dollars": "0.45"
                                    },
                                    "price": {
                                        "open_dollars": "0.41",
                                        "low_dollars": "0.39",
                                        "high_dollars": "0.46",
                                        "close_dollars": "0.44",
                                        "mean_dollars": "0.42",
                                        "previous_dollars": "0.40",
                                        "min_dollars": "0.39",
                                        "max_dollars": "0.46"
                                    },
                                    "volume_fp": "500.00",
                                    "open_interest_fp": "2000.00"
                                }
                            ]
                        },
                        {
                            "market_ticker": "MARKET-B",
                            "candlesticks": [
                                {
                                    "end_period_ts": 1704067200,
                                    "yes_bid": {
                                        "open_dollars": "0.70",
                                        "low_dollars": "0.68",
                                        "high_dollars": "0.75",
                                        "close_dollars": "0.72"
                                    },
                                    "yes_ask": {
                                        "open_dollars": "0.72",
                                        "low_dollars": "0.70",
                                        "high_dollars": "0.77",
                                        "close_dollars": "0.74"
                                    },
                                    "price": {
                                        "open_dollars": null,
                                        "low_dollars": null,
                                        "high_dollars": null,
                                        "close_dollars": null,
                                        "mean_dollars": null,
                                        "previous_dollars": null,
                                        "min_dollars": null,
                                        "max_dollars": null
                                    },
                                    "volume_fp": "0.00",
                                    "open_interest_fp": "800.00"
                                }
                            ]
                        }
                    ]
                }
                """));

        var query = new BatchMarketCandlesticksQuery
        {
            MarketTickers = ["MARKET-A", "MARKET-B"],
            StartTimestamp = startTs,
            EndTimestamp = endTs,
            PeriodInterval = PeriodInterval.OnDay
        };

        // Act
        var result = await _client.GetBatchMarketCandlesticksAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Markets.Should().HaveCount(2);

        var marketA = result.Markets[0];
        marketA.MarketTicker.Should().Be("MARKET-A");
        marketA.Candlesticks.Should().HaveCount(1);

        var candleA = marketA.Candlesticks[0];
        candleA.EndPeriodTimestamp.Should().Be(1704067200);
        candleA.VolumeFp.Should().Be("500.00");
        candleA.OpenInterestFp.Should().Be("2000.00");
        candleA.YesBid.OpenDollars.Should().Be("0.40");
        candleA.YesBid.LowDollars.Should().Be("0.38");
        candleA.YesBid.HighDollars.Should().Be("0.45");
        candleA.YesBid.CloseDollars.Should().Be("0.43");
        candleA.YesAsk.OpenDollars.Should().Be("0.42");
        candleA.YesAsk.LowDollars.Should().Be("0.40");
        candleA.YesAsk.HighDollars.Should().Be("0.47");
        candleA.YesAsk.CloseDollars.Should().Be("0.45");
        candleA.Price.OpenDollars.Should().Be("0.41");
        candleA.Price.LowDollars.Should().Be("0.39");
        candleA.Price.HighDollars.Should().Be("0.46");
        candleA.Price.CloseDollars.Should().Be("0.44");
        candleA.Price.MeanDollars.Should().Be("0.42");
        candleA.Price.PreviousDollars.Should().Be("0.40");
        candleA.Price.MinDollars.Should().Be("0.39");
        candleA.Price.MaxDollars.Should().Be("0.46");

        var marketB = result.Markets[1];
        marketB.MarketTicker.Should().Be("MARKET-B");
        marketB.Candlesticks.Should().HaveCount(1);

        var candleB = marketB.Candlesticks[0];
        candleB.VolumeFp.Should().Be("0.00");
        candleB.OpenInterestFp.Should().Be("800.00");
        candleB.Price.OpenDollars.Should().BeNull();
        candleB.Price.PreviousDollars.Should().BeNull();
    }

    [Fact]
    public async Task GetBatchMarketCandlesticksAsync_WithEmptyMarketTickers_ThrowsArgumentException()
    {
        // Arrange
        var query = new BatchMarketCandlesticksQuery
        {
            MarketTickers = [],
            StartTimestamp = DateTimeOffset.UtcNow.AddDays(-1),
            EndTimestamp = DateTimeOffset.UtcNow,
            PeriodInterval = PeriodInterval.OnDay
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetBatchMarketCandlesticksAsync(query));
    }

    [Fact]
    public async Task GetBatchMarketCandlesticksAsync_WithIncludeLatestBeforeStart_AppliesParameter()
    {
        // Arrange
        var startTs = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endTs = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);

        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/markets/candlesticks")
                .WithParam("include_latest_before_start", "true")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "markets": []
                }
                """));

        var query = new BatchMarketCandlesticksQuery
        {
            MarketTickers = ["MARKET-A"],
            StartTimestamp = startTs,
            EndTimestamp = endTs,
            PeriodInterval = PeriodInterval.OnDay,
            IncludeLatestBeforeStart = true
        };

        // Act
        var result = await _client.GetBatchMarketCandlesticksAsync(query);

        // Assert
        result.Should().NotBeNull();
        _server.LogEntries.Should().HaveCount(1);
    }
}
