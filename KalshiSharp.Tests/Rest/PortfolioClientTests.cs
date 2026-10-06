using System.Globalization;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Tests.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Http;
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Requests;
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
                        "positions": [
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
                        "positions": [
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
                .WithBody("""{"positions": [], "cursor": null}"""));

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
                        "fills": [
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
                .WithBody("""{"fills": [], "cursor": null}"""));

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
                .WithBody("""{"fills": [], "cursor": null}"""));

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
                .WithBody("""{"fills": [], "cursor": null}"""));

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
                .WithHeader(MockRequestSigner.AccessKeyHeader, "test-api-key")
                .WithHeader(MockRequestSigner.AccessTimestampHeader, "*")
                .WithHeader(MockRequestSigner.AccessSignatureHeader, "*")
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

    [Fact]
    public async Task GetBalanceAsync_CurrentPayload_ParsesDollarBalance()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/balance")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{ "balance": 5600, "balance_dollars": "56.0000", "portfolio_value": 7000, "updated_ts": 1755600000 }"""));

        var result = await _portfolioClient.GetBalanceAsync();

        result.Balance.Should().Be(5600);
        result.BalanceDollars.Should().Be("56.0000");
    }

    [Fact]
    public async Task GetBalanceAsync_WithCurrentScope_AppliesExplicitZeroValues()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/balance")
                .WithParam("subaccount", "0")
                .WithParam("exchange_index", "0")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{ "balance": 5600, "balance_dollars": "56.0000", "portfolio_value": 7000, "updated_ts": 1755600000 }"""));

        var result = await _portfolioClient.GetBalanceAsync(new BalanceQuery
        {
            Subaccount = 0,
            ExchangeIndex = 0
        });

        result.BalanceDollars.Should().Be("56.0000");
    }

    [Fact]
    public async Task ListPositionsAsync_CurrentPayload_ParsesMarketAndEventPositions()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/positions")
                .WithParam("count_filter", "position,total_traded")
                .WithParam("subaccount", "0")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "market_positions": [{
                        "ticker": "MARKET-1",
                        "total_traded_dollars": "5.6000",
                        "position_fp": "10.00",
                        "market_exposure_dollars": "2.5000",
                        "realized_pnl_dollars": "0.1000",
                        "fees_paid_dollars": "0.0200",
                        "last_updated_ts": "2026-08-19T12:00:00Z"
                    }],
                    "event_positions": [{
                        "event_ticker": "EVENT-1",
                        "total_cost_dollars": "5.6000",
                        "total_cost_shares_fp": "10.00",
                        "event_exposure_dollars": "2.5000",
                        "realized_pnl_dollars": "0.1000",
                        "fees_paid_dollars": "0.0200"
                    }],
                    "cursor": null
                }
                """));

        var result = await _portfolioClient.ListPositionsAsync(new PositionQuery
        {
            CountFilter = ["position", "total_traded"],
            Subaccount = 0
        });

        result.Items.Should().ContainSingle();
        result.Items[0].PositionFp.Should().Be("10.00");
        result.EventPositions.Should().ContainSingle().Which.EventTicker.Should().Be("EVENT-1");
    }

    [Fact]
    public async Task ListFillsAsync_CurrentPayload_ParsesFixedPointFieldsAndTimeFilters()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/fills")
                .WithParam("min_ts", "1755600000")
                .WithParam("max_ts", "1755603600")
                .WithParam("subaccount", "2")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "fills": [{
                        "fill_id": "fill-1",
                        "trade_id": "trade-1",
                        "order_id": "order-1",
                        "ticker": "MARKET-1",
                        "market_ticker": "MARKET-1",
                        "count_fp": "10.00",
                        "yes_price_dollars": "0.5600",
                        "no_price_dollars": "0.4400",
                        "is_taker": true,
                        "fee_cost": "0.0200",
                        "created_time": "2026-08-19T12:00:00Z",
                        "ts": 1755600000
                    }],
                    "cursor": null
                }
                """));

        var result = await _portfolioClient.ListFillsAsync(new FillQuery
        {
            MinTime = DateTimeOffset.FromUnixTimeSeconds(1755600000),
            MaxTime = DateTimeOffset.FromUnixTimeSeconds(1755603600),
            Subaccount = 2
        });

        result.Items.Should().ContainSingle();
        result.Items[0].CountFp.Should().Be("10.00");
        result.Items[0].YesPriceDollars.Should().Be("0.5600");
        result.Items[0].FeeCost.Should().Be("0.0200");
    }

    [Fact]
    public async Task GetSubaccountBalancesAsync_ParsesShardEntries()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/subaccounts/balances")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"subaccount_balances":[{"subaccount_number":2,"exchange_index":1,"balance":"125.5000","updated_ts":1755600000}]}"""));

        var result = await _portfolioClient.GetSubaccountBalancesAsync();

        result.SubaccountBalances.Should().ContainSingle();
        result.SubaccountBalances[0].SubaccountNumber.Should().Be(2);
        result.SubaccountBalances[0].ExchangeIndex.Should().Be(1);
        result.SubaccountBalances[0].Balance.Should().Be("125.5000");
    }

    [Fact]
    public async Task CreateSubaccountAsync_SendsOptionalExchangeIndexAndParsesNumber()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/subaccounts")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"subaccount_number":7}"""));

        var result = await _portfolioClient.CreateSubaccountAsync(new CreateSubaccountRequest
        {
            ExchangeIndex = 2
        });

        result.SubaccountNumber.Should().Be(7);
        _server.LogEntries.Should().ContainSingle();
        var requestBody = _server.LogEntries[0].RequestMessage!.Body;
        requestBody.Should().NotBeNull();
        using var body = JsonDocument.Parse(requestBody!);
        body.RootElement.GetProperty("exchange_index").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task CreateSubaccountAsync_AllowsOmittedRequestBody()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/subaccounts")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"subaccount_number":1}"""));

        var result = await _portfolioClient.CreateSubaccountAsync();

        result.SubaccountNumber.Should().Be(1);
    }

    [Fact]
    public async Task TransferBetweenSubaccountsAsync_SendsCurrentRequestShape()
    {
        var transferId = Guid.Parse("3c90c3cc-0d44-4b50-8888-8dd25736052a");
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/subaccounts/transfer")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{}"));

        await _portfolioClient.TransferBetweenSubaccountsAsync(new CreateSubaccountTransferRequest
        {
            ClientTransferId = transferId,
            FromSubaccount = 0,
            ToSubaccount = 63,
            AmountCents = 12_345,
            ExchangeIndex = 2
        });

        _server.LogEntries.Should().ContainSingle();
        var requestBody = _server.LogEntries[0].RequestMessage!.Body;
        requestBody.Should().NotBeNull();
        using var body = JsonDocument.Parse(requestBody!);
        body.RootElement.GetProperty("client_transfer_id").GetString().Should().Be(transferId.ToString());
        body.RootElement.GetProperty("from_subaccount").GetInt32().Should().Be(0);
        body.RootElement.GetProperty("to_subaccount").GetInt32().Should().Be(63);
        body.RootElement.GetProperty("amount_cents").GetInt64().Should().Be(12_345);
        body.RootElement.GetProperty("exchange_index").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task TransferBetweenSubaccountsAsync_OmitsOptionalExchangeIndex()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/subaccounts/transfer")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{}"));

        await _portfolioClient.TransferBetweenSubaccountsAsync(new CreateSubaccountTransferRequest
        {
            ClientTransferId = Guid.Parse("3c90c3cc-0d44-4b50-8888-8dd25736052a"),
            FromSubaccount = 0,
            ToSubaccount = 1,
            AmountCents = 1
        });

        var requestBody = _server.LogEntries[0].RequestMessage!.Body;
        requestBody.Should().NotBeNull();
        using var body = JsonDocument.Parse(requestBody!);
        body.RootElement.TryGetProperty("exchange_index", out _).Should().BeFalse();
    }

    [Fact]
    public async Task TransferBetweenSubaccountsAsync_ValidatesDocumentedBoundaries()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _portfolioClient.TransferBetweenSubaccountsAsync(new CreateSubaccountTransferRequest
            {
                ClientTransferId = Guid.Empty,
                FromSubaccount = 0,
                ToSubaccount = 1,
                AmountCents = 1
            }));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _portfolioClient.TransferBetweenSubaccountsAsync(new CreateSubaccountTransferRequest
            {
                ClientTransferId = Guid.NewGuid(),
                FromSubaccount = -1,
                ToSubaccount = 1,
                AmountCents = 1
            }));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _portfolioClient.TransferBetweenSubaccountsAsync(new CreateSubaccountTransferRequest
            {
                ClientTransferId = Guid.NewGuid(),
                FromSubaccount = 0,
                ToSubaccount = 64,
                AmountCents = 1
            }));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _portfolioClient.TransferBetweenSubaccountsAsync(new CreateSubaccountTransferRequest
            {
                ClientTransferId = Guid.NewGuid(),
                FromSubaccount = 1,
                ToSubaccount = 1,
                AmountCents = 1
            }));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _portfolioClient.TransferBetweenSubaccountsAsync(new CreateSubaccountTransferRequest
            {
                ClientTransferId = Guid.NewGuid(),
                FromSubaccount = 0,
                ToSubaccount = 1,
                AmountCents = 0
            }));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _portfolioClient.TransferBetweenSubaccountsAsync(new CreateSubaccountTransferRequest
            {
                ClientTransferId = Guid.NewGuid(),
                FromSubaccount = 0,
                ToSubaccount = 1,
                AmountCents = 1,
                ExchangeIndex = -1
            }));
    }

    [Fact]
    public async Task ListSubaccountTransfersAsync_ParsesTransfersAndPagination()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/subaccounts/transfers")
                .WithParam("limit", "1000")
                .WithParam("cursor", "page-2")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "transfers": [
                        {
                            "transfer_id": "transfer-1",
                            "from_subaccount": 0,
                            "to_subaccount": 2,
                            "amount_cents": 2147483648,
                            "created_ts": 1755600000,
                            "exchange_index": 3
                        }
                    ],
                    "cursor": "next-page"
                }
                """));

        var result = await _portfolioClient.ListSubaccountTransfersAsync(new SubaccountTransferQuery
        {
            Limit = 1000,
            Cursor = "page-2"
        });

        result.Items.Should().ContainSingle();
        result.Cursor.Should().Be("next-page");
        result.HasMore.Should().BeTrue();
        result.Items[0].TransferId.Should().Be("transfer-1");
        result.Items[0].FromSubaccount.Should().Be(0);
        result.Items[0].ToSubaccount.Should().Be(2);
        result.Items[0].AmountCents.Should().Be(2_147_483_648);
        result.Items[0].CreatedTs.Should().Be(1755600000);
        result.Items[0].ExchangeIndex.Should().Be(3);
    }

    [Fact]
    public async Task GetSubaccountNettingAsync_ParsesConfigs()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/subaccounts/netting")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "netting_configs": [
                        { "subaccount_number": 0, "enabled": true, "exchange_index": 0 },
                        { "subaccount_number": 3, "enabled": false, "exchange_index": 2 }
                    ]
                }
                """));

        var result = await _portfolioClient.GetSubaccountNettingAsync();

        result.NettingConfigs.Should().HaveCount(2);
        result.NettingConfigs[0].SubaccountNumber.Should().Be(0);
        result.NettingConfigs[0].Enabled.Should().BeTrue();
        result.NettingConfigs[1].SubaccountNumber.Should().Be(3);
        result.NettingConfigs[1].ExchangeIndex.Should().Be(2);
    }

    [Fact]
    public async Task UpdateSubaccountNettingAsync_SendsCurrentRequestShape()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/subaccounts/netting")
                .UsingPut())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json"));

        await _portfolioClient.UpdateSubaccountNettingAsync(new UpdateSubaccountNettingRequest
        {
            SubaccountNumber = 63,
            Enabled = true
        });

        _server.LogEntries.Should().ContainSingle();
        var requestBody = _server.LogEntries[0].RequestMessage!.Body;
        requestBody.Should().NotBeNull();
        using var body = JsonDocument.Parse(requestBody!);
        body.RootElement.GetProperty("subaccount_number").GetInt32().Should().Be(63);
        body.RootElement.GetProperty("enabled").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetTotalRestingOrderValueAsync_ParsesExchangeBreakdown()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/summary/total_resting_order_value")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "total_resting_order_value": 2147483648,
                    "resting_order_value_breakdown": [
                        { "exchange_index": 0, "balance": "0.5600" },
                        { "exchange_index": 2, "balance": "12.3400" }
                    ]
                }
                """));

        var result = await _portfolioClient.GetTotalRestingOrderValueAsync();

        result.TotalRestingOrderValue.Should().Be(2_147_483_648);
        result.RestingOrderValueBreakdown.Should().HaveCount(2);
        result.RestingOrderValueBreakdown[0].ExchangeIndex.Should().Be(0);
        result.RestingOrderValueBreakdown[0].Balance.Should().Be("0.5600");
        result.RestingOrderValueBreakdown[1].ExchangeIndex.Should().Be(2);
        result.RestingOrderValueBreakdown[1].Balance.Should().Be("12.3400");
    }

    [Fact]
    public async Task GetTargetBalanceAllocationAsync_ParsesRestingMarginReservation()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/target_balance_allocation")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "allocations": [
                        { "exchange_index": 0, "percent": 70 },
                        { "exchange_index": 2, "percent": 30 }
                    ],
                    "resting_margin_reservation": "none"
                }
                """));

        var result = await _portfolioClient.GetTargetBalanceAllocationAsync();

        result.Allocations.Should().HaveCount(2);
        result.Allocations[0].ExchangeIndex.Should().Be(0);
        result.Allocations[0].Percent.Should().Be(70);
        result.RestingMarginReservation.Should().Be(RestingMarginReservation.None);
    }

    [Fact]
    public async Task SetTargetBalanceAllocationAsync_SendsAllocationsAndRestingPolicy()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/target_balance_allocation")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{}"));

        await _portfolioClient.SetTargetBalanceAllocationAsync(new SetTargetBalanceAllocationRequest
        {
            Allocations =
            [
                new TargetBalanceAllocationRequest { ExchangeIndex = 0, Percent = 60 },
                new TargetBalanceAllocationRequest { ExchangeIndex = 3, Percent = 40 }
            ],
            RestingMarginReservation = RestingMarginReservation.Max
        });

        _server.LogEntries.Should().ContainSingle();
        var requestBody = _server.LogEntries[0].RequestMessage!.Body;
        requestBody.Should().NotBeNull();
        using var body = JsonDocument.Parse(requestBody!);
        body.RootElement.GetProperty("resting_margin_reservation").GetString().Should().Be("max");
        body.RootElement.GetProperty("allocations")[0].GetProperty("exchange_index").GetInt32().Should().Be(0);
        body.RootElement.GetProperty("allocations")[1].GetProperty("percent").GetInt32().Should().Be(40);
    }

    [Fact]
    public async Task SetTargetBalanceAllocationAsync_AllowsEmptyAllocationsToDisableRebalancing()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/target_balance_allocation")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{}"));

        await _portfolioClient.SetTargetBalanceAllocationAsync(new SetTargetBalanceAllocationRequest
        {
            Allocations = [],
            RestingMarginReservation = RestingMarginReservation.None
        });

        var requestBody = _server.LogEntries[0].RequestMessage!.Body;
        requestBody.Should().NotBeNull();
        using var body = JsonDocument.Parse(requestBody!);
        body.RootElement.GetProperty("allocations").GetArrayLength().Should().Be(0);
        body.RootElement.GetProperty("resting_margin_reservation").GetString().Should().Be("none");
    }

    [Fact]
    public async Task SetTargetBalanceAllocationAsync_ValidatesDocumentedAllocationRules()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _portfolioClient.SetTargetBalanceAllocationAsync(new SetTargetBalanceAllocationRequest
            {
                Allocations = [new TargetBalanceAllocationRequest { ExchangeIndex = 0, Percent = 99 }]
            }));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _portfolioClient.SetTargetBalanceAllocationAsync(new SetTargetBalanceAllocationRequest
            {
                Allocations = [new TargetBalanceAllocationRequest { ExchangeIndex = -1, Percent = 100 }]
            }));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _portfolioClient.SetTargetBalanceAllocationAsync(new SetTargetBalanceAllocationRequest
            {
                Allocations = [new TargetBalanceAllocationRequest { ExchangeIndex = 0, Percent = 101 }]
            }));
    }

    [Fact]
    public async Task PositionAndFillQueries_PreserveZeroExchangeIndex()
    {
        _server.Given(Request.Create().WithPath("/trade-api/v2/portfolio/positions")
                .WithParam("exchange_index", "0").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody("""{"market_positions":[],"event_positions":[],"cursor":null}"""));
        _server.Given(Request.Create().WithPath("/trade-api/v2/portfolio/fills")
                .WithParam("exchange_index", "0").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody("""{"fills":[],"cursor":null}"""));

        await _portfolioClient.ListPositionsAsync(new PositionQuery { ExchangeIndex = 0 });
        await _portfolioClient.ListFillsAsync(new FillQuery { ExchangeIndex = 0 });
    }

    [Theory]
    [InlineData(PositionSettlementStatus.Unsettled, "unsettled")]
    [InlineData(PositionSettlementStatus.Settled, "settled")]
    [InlineData(PositionSettlementStatus.All, "all")]
    public async Task ListPositionsAsync_IncludesSettlementStatusFilter(
        PositionSettlementStatus settlementStatus,
        string expectedQueryValue)
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/positions")
                .WithParam("settlement_status", expectedQueryValue)
                .WithParam("count_filter", "position,total_traded")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"market_positions":[],"event_positions":[],"cursor":null}"""));

        var result = await _portfolioClient.ListPositionsAsync(new PositionQuery
        {
            SettlementStatus = settlementStatus,
            CountFilter = ["position", "total_traded"]
        });

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListSettlementsAsync_ParsesCurrentPayloadAndQuery()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/settlements")
                .WithParam("limit", "500")
                .WithParam("cursor", "settlement-page")
                .WithParam("ticker", "MARKET-1")
                .WithParam("event_ticker", "EVENT-1")
                .WithParam("min_ts", "1755600000")
                .WithParam("max_ts", "1755686400")
                .WithParam("subaccount", "63")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "settlements": [
                        {
                            "ticker": "MARKET-1",
                            "exchange_index": 2,
                            "event_ticker": "EVENT-1",
                            "market_result": "yes",
                            "yes_count_fp": "10.00",
                            "yes_total_cost_dollars": "0.5600",
                            "no_count_fp": "0.00",
                            "no_total_cost_dollars": "0.0000",
                            "revenue": 2147483648,
                            "settled_time": "2026-08-19T12:00:00Z",
                            "fee_cost": "0.3400",
                            "value": 100
                        }
                    ],
                    "cursor": null
                }
                """));

        var result = await _portfolioClient.ListSettlementsAsync(new SettlementQuery
        {
            Limit = 500,
            Cursor = "settlement-page",
            Ticker = "MARKET-1",
            EventTicker = "EVENT-1",
            MinTime = DateTimeOffset.FromUnixTimeSeconds(1755600000),
            MaxTime = DateTimeOffset.FromUnixTimeSeconds(1755686400),
            Subaccount = 63
        });

        result.Items.Should().ContainSingle();
        result.HasMore.Should().BeFalse();
        result.Items[0].Ticker.Should().Be("MARKET-1");
        result.Items[0].ExchangeIndex.Should().Be(2);
        result.Items[0].EventTicker.Should().Be("EVENT-1");
        result.Items[0].MarketResult.Should().Be(OrderSide.Yes);
        result.Items[0].YesCountFp.Should().Be("10.00");
        result.Items[0].YesTotalCostDollars.Should().Be("0.5600");
        result.Items[0].NoCountFp.Should().Be("0.00");
        result.Items[0].NoTotalCostDollars.Should().Be("0.0000");
        result.Items[0].Revenue.Should().Be(2_147_483_648);
        result.Items[0].SettledTime.Should().Be(DateTimeOffset.Parse("2026-08-19T12:00:00Z", CultureInfo.InvariantCulture));
        result.Items[0].FeeCost.Should().Be("0.3400");
        result.Items[0].Value.Should().Be(100);
    }

    [Fact]
    public async Task ListSettlementsAsync_OmitsSubaccountByDefault()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/settlements")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"settlements":[],"cursor":null}"""));

        await _portfolioClient.ListSettlementsAsync();

        _server.LogEntries.Should().ContainSingle();
        _server.LogEntries[0].RequestMessage!.Query.Should().BeNullOrEmpty();
    }

    [Fact]
    public void PortfolioHistoryQueries_ValidateRanges()
    {
        var invalidSettlementLimit = () => new SettlementQuery { Limit = 0 }.ToQueryString();
        var invalidSettlementSubaccount = () => new SettlementQuery { Subaccount = 64 }.ToQueryString();
        var invalidTransferLimit = () => new SubaccountTransferQuery { Limit = 1001 }.ToQueryString();

        invalidSettlementLimit.Should().Throw<ArgumentOutOfRangeException>();
        invalidSettlementSubaccount.Should().Throw<ArgumentOutOfRangeException>();
        invalidTransferLimit.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task SubaccountAdministrationAsync_ValidatesDocumentedBoundaries()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _portfolioClient.CreateSubaccountAsync(new CreateSubaccountRequest { ExchangeIndex = -1 }));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _portfolioClient.UpdateSubaccountNettingAsync(new UpdateSubaccountNettingRequest
            {
                SubaccountNumber = 64,
                Enabled = true
            }));
    }
}
