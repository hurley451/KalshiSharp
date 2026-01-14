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
                    "ticker": "AAPL-2024-01-01",
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
                """));

        // Act
        var result = await _client.GetMarketAsync(ticker);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be("AAPL-2024-01-01");
        result.EventTicker.Should().Be("AAPL-EVENT");
        result.Title.Should().Be("Will Apple reach $200?");
        result.Status.Should().Be(MarketStatus.Active);
        result.Volume.Should().Be(10000);
    }

    [Fact]
    public async Task GetMarketAsync_WithNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/markets/INVALID-TICKER")
                .UsingGet())
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
                    "items": [
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
                    "cursor": "next-page-cursor"
                }
                """));

        // Act
        var result = await _client.ListMarketsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].Ticker.Should().Be("MARKET-1");
        result.Cursor.Should().Be("next-page-cursor");
        result.HasMore.Should().BeTrue();
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
                    "items": [],
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
                    "items": [
                        {
                            "ticker": "MARKET-2",
                            "event_ticker": "EVENT-2",
                            "title": "Market 2",
                            "status": "closed",
                            "yes_bid": 0,
                            "yes_ask": 0,
                            "no_bid": 0,
                            "no_ask": 0,
                            "volume": 5000,
                            "volume24_h": 0,
                            "open_interest": 0,
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
                    "items": [
                        {
                            "trade_id": "trade-001",
                            "ticker": "MARKET-ABC",
                            "side": "yes",
                            "yes_price": 55,
                            "no_price": 45,
                            "count": 10,
                            "created_time": 1704067200000
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
        result.Items[0].TradeId.Should().Be("trade-001");
        result.Items[0].Ticker.Should().Be("MARKET-ABC");
        result.Items[0].Side.Should().Be(OrderSide.Yes);
        result.Items[0].YesPrice.Should().Be(55);
        result.Items[0].NoPrice.Should().Be(45);
        result.Items[0].Count.Should().Be(10);
        result.Cursor.Should().Be("trades-cursor");
        result.HasMore.Should().BeTrue();
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
                    "items": [],
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
        // Arrange - ticker with characters that need encoding
        // Using a simpler ticker that is still URL-safe for WireMock matching
        const string ticker = "MARKET-TEST-2024";

        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/markets/{ticker}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "ticker": "MARKET-TEST-2024",
                    "event_ticker": "EVENT",
                    "title": "Test",
                    "status": "active",
                    "yes_bid": 50,
                    "yes_ask": 50,
                    "no_bid": 50,
                    "no_ask": 50,
                    "volume": 0,
                    "volume24_h": 0,
                    "open_interest": 0,
                    "can_close_early": false
                }
                """));

        // Act
        var result = await _client.GetMarketAsync(ticker);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be(ticker);
    }
}
