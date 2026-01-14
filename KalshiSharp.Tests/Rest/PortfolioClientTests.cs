using System.Globalization;
using System.Net;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Http;
using KalshiSharp.Models.Enums;
using KalshiSharp.Rest.Portfolio;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Rest;

/// <summary>
/// HTTP contract tests for the Portfolio client.
/// </summary>
public sealed class PortfolioClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly PortfolioClient _portfolioClient;
    private readonly IKalshiRequestSigner _signer;

    public PortfolioClientTests()
    {
        _server = WireMockServer.Start();

        var options = Options.Create(new KalshiClientOptions
        {
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret",
            BaseUri = new Uri(_server.Url!),
            Timeout = TimeSpan.FromSeconds(5)
        });

        _signer = new HmacSha256RequestSigner(options.Value.ApiKey, options.Value.ApiSecret);
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

        _portfolioClient = new PortfolioClient(kalshiHttpClient);
    }

    public void Dispose()
    {
        _server.Dispose();
        (_signer as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsBalance()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/balance")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "balance": 100000,
                        "portfolio_value": 25000,
                        "updated_ts": 1704067200
                    }
                    """));

        // Act
        var result = await _portfolioClient.GetBalanceAsync();

        // Assert
        result.Should().NotBeNull();
        result.Balance.Should().Be(100000);
        result.PortfolioValue.Should().Be(25000);
        result.UpdatedTs.Should().Be(1704067200);
    }

    [Fact]
    public async Task ListPositionsAsync_ReturnsPositions()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/positions")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "items": [
                            {
                                "ticker": "TICKER-1",
                                "event_ticker": "EVENT-1",
                                "market_exposure": 100,
                                "position": 100,
                                "yes_contracts": 10,
                                "no_contracts": 0,
                                "average_price_paid": 50,
                                "total_cost": 500,
                                "realized_pnl": 0
                            }
                        ],
                        "cursor": "next-cursor"
                    }
                    """));

        // Act
        var result = await _portfolioClient.ListPositionsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Cursor.Should().Be("next-cursor");
        result.HasMore.Should().BeTrue();

        var position = result.Items[0];
        position.Ticker.Should().Be("TICKER-1");
        position.EventTicker.Should().Be("EVENT-1");
        position.MarketExposure.Should().Be(100);
        position.Position.Should().Be(100);
        position.YesContracts.Should().Be(10);
        position.NoContracts.Should().Be(0);
        position.AveragePricePaid.Should().Be(50);
        position.TotalCost.Should().Be(500);
        position.RealizedPnl.Should().Be(0);
    }

    [Fact]
    public async Task ListPositionsAsync_WithPagination_IncludesQueryParams()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/positions")
                .WithParam("cursor", "page-2")
                .WithParam("limit", "10")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"items": [], "cursor": null}"""));

        // Act
        var result = await _portfolioClient.ListPositionsAsync(cursor: "page-2", limit: 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ListPositionsAsync_WithTickerFilter_IncludesQueryParam()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/positions")
                .WithParam("ticker", "SPECIFIC-TICKER")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "items": [
                            {
                                "ticker": "SPECIFIC-TICKER",
                                "event_ticker": "EVENT-1",
                                "market_exposure": 50,
                                "position": 50,
                                "yes_contracts": 5,
                                "no_contracts": 0
                            }
                        ],
                        "cursor": null
                    }
                    """));

        // Act
        var result = await _portfolioClient.ListPositionsAsync(ticker: "SPECIFIC-TICKER");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].Ticker.Should().Be("SPECIFIC-TICKER");
    }

    [Fact]
    public async Task ListPositionsAsync_WithEventTickerFilter_IncludesQueryParam()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/positions")
                .WithParam("event_ticker", "EVENT-123")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"items": [], "cursor": null}"""));

        // Act
        var result = await _portfolioClient.ListPositionsAsync(eventTicker: "EVENT-123");

        // Assert
        result.Should().NotBeNull();
        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListFillsAsync_ReturnsFills()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/fills")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "items": [
                            {
                                "trade_id": "trade-123",
                                "order_id": "order-456",
                                "ticker": "TICKER-1",
                                "side": "yes",
                                "action": "buy",
                                "count": 5,
                                "yes_price": 50,
                                "no_price": 50,
                                "is_taker": true,
                                "created_time": "2026-01-10T10:00:00Z"
                            }
                        ],
                        "cursor": "fill-cursor"
                    }
                    """));

        // Act
        var result = await _portfolioClient.ListFillsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Cursor.Should().Be("fill-cursor");
        result.HasMore.Should().BeTrue();

        var fill = result.Items[0];
        fill.TradeId.Should().Be("trade-123");
        fill.OrderId.Should().Be("order-456");
        fill.Ticker.Should().Be("TICKER-1");
        fill.Side.Should().Be(OrderSide.Yes);
        fill.Action.Should().Be("buy");
        fill.Count.Should().Be(5);
        fill.YesPrice.Should().Be(50);
        fill.NoPrice.Should().Be(50);
        fill.IsTaker.Should().BeTrue();
        fill.CreatedTime.Should().Be(DateTimeOffset.Parse("2026-01-10T10:00:00Z", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ListFillsAsync_WithPagination_IncludesQueryParams()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/fills")
                .WithParam("cursor", "fill-page-2")
                .WithParam("limit", "25")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"items": [], "cursor": null}"""));

        // Act
        var result = await _portfolioClient.ListFillsAsync(cursor: "fill-page-2", limit: 25);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ListFillsAsync_WithTickerFilter_IncludesQueryParam()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/fills")
                .WithParam("ticker", "FILTERED-TICKER")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"items": [], "cursor": null}"""));

        // Act
        var result = await _portfolioClient.ListFillsAsync(ticker: "FILTERED-TICKER");

        // Assert
        result.Should().NotBeNull();
        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListFillsAsync_WithOrderIdFilter_IncludesQueryParam()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/fills")
                .WithParam("order_id", "specific-order")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"items": [], "cursor": null}"""));

        // Act
        var result = await _portfolioClient.ListFillsAsync(orderId: "specific-order");

        // Assert
        result.Should().NotBeNull();
        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBalanceAsync_IncludesAuthHeaders()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/balance")
                .WithHeader(HmacSha256RequestSigner.AccessKeyHeader, "test-api-key")
                .WithHeader(HmacSha256RequestSigner.AccessTimestampHeader, "*")
                .WithHeader(HmacSha256RequestSigner.AccessSignatureHeader, "*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"balance": 100000, "portfolio_value": 25000, "updated_ts": 1704067200}"""));

        // Act
        var result = await _portfolioClient.GetBalanceAsync();

        // Assert
        result.Should().NotBeNull();
        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBalanceAsync_SupportsCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _portfolioClient.GetBalanceAsync(cts.Token));
    }

    [Fact]
    public async Task ListPositionsAsync_SupportsCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _portfolioClient.ListPositionsAsync(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ListFillsAsync_SupportsCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _portfolioClient.ListFillsAsync(cancellationToken: cts.Token));
    }
}
