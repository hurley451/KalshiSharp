using System.Globalization;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Tests.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Errors;
using KalshiSharp.Http;
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Requests;
using KalshiSharp.Rest.Orders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Orders;

/// <summary>
/// HTTP contract tests for the Order client.
/// </summary>
public sealed class OrderClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly OrderClient _client;
    private readonly IKalshiRequestSigner _signer;

    public OrderClientTests()
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

        _client = new OrderClient(kalshiHttpClient);
    }

    public void Dispose()
    {
        _server.Dispose();
        (_signer as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task CreateOrderAsync_ReturnsCreatedOrder()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .WithBody(body => body != null && body.Contains("\"ticker\"") && body.Contains("\"MARKET-ABC\""))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "order": {
                        "order_id": "order-12345",
                        "user_id": "user-abc",
                        "client_order_id": "client-001",
                        "ticker": "MARKET-ABC",
                        "side": "yes",
                        "type": "limit",
                        "status": "resting",
                        "action": "buy",
                        "fill_count_fp": "0.00",
                        "initial_count_fp": "10.00",
                        "remaining_count_fp": "10.00",
                        "yes_price_dollars": "0.55",
                        "no_price_dollars": "0.45",
                        "taker_fill_cost_dollars": "0.00",
                        "maker_fill_cost_dollars": "0.00",
                        "taker_fees_dollars": "0.00",
                        "maker_fees_dollars": "0.00",
                        "queue_position": 1,
                        "created_time": "2024-01-01T00:00:00Z",
                        "expiration_time": "2024-12-31T23:59:59Z",
                        "self_trade_prevention_type": "cancel_resting",
                        "order_group_id": "group-xyz",
                        "cancel_order_on_pause": false,
                        "outcome_side": "yes",
                        "book_side": "bid"
                    }
                }
                """));

        var request = new CreateOrderRequest
        {
            Ticker = "MARKET-ABC",
            Side = OrderSide.Yes,
            Action = "buy",
            CountFp = "10.00",
            Type = OrderType.Limit,
            YesPriceDollars = "0.55",
            NoPriceDollars = null,
            TimeInForce = TimeInForce.GoodTillCanceled,
            ExpirationTime = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero),
            ClientOrderId = "client-001",
            SellPositionFloor = false,
            BuyMaxCost = false
        };

        // Act
        var result = await _client.CreateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be("order-12345");
        result.UserId.Should().Be("user-abc");
        result.ClientOrderId.Should().Be("client-001");
        result.Ticker.Should().Be("MARKET-ABC");
        result.Side.Should().Be(OrderSide.Yes);
        result.Type.Should().Be(OrderType.Limit);
        result.Status.Should().Be(OrderStatus.Resting);
        result.Action.Should().Be("buy");
        result.FillCountFp.Should().Be("0.00");
        result.InitialCountFp.Should().Be("10.00");
        result.RemainingCountFp.Should().Be("10.00");
        result.YesPriceDollars.Should().Be("0.55");
        result.NoPriceDollars.Should().Be("0.45");
        result.TakerFillCostDollars.Should().Be("0.00");
        result.MakerFillCostDollars.Should().Be("0.00");
        result.TakerFeesDollars.Should().Be("0.00");
        result.MakerFeesDollars.Should().Be("0.00");
        result.QueuePosition.Should().Be(1);
        result.CreatedTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        result.ExpirationTime.Should().Be(new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero));
        result.SelfTradePreventionType.Should().Be("cancel_resting");
        result.OrderGroupId.Should().Be("group-xyz");
        result.CancelOrderOnPause.Should().BeFalse();
        result.OutcomeSide.Should().Be(OrderSide.Yes);
        result.BookSide.Should().Be(OrderBookSide.Bid);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.CreateOrderAsync(null!));
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidationError_ThrowsValidationException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"validation_error","message":"Invalid order","errors":{"count":["must be positive"]}}"""));

        var request = new CreateOrderRequest
        {
            Ticker = "MARKET-ABC",
            Side = OrderSide.Yes,
            Action = "buy",
            CountFp = "-1.00",
            Type = OrderType.Limit,
            YesPriceDollars = "0.55"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiValidationException>(
            () => _client.CreateOrderAsync(request));

        exception.ValidationErrors.Should().ContainKey("count");
    }

    [Fact]
    public async Task AmendOrderAsync_ReturnsUpdatedOrder()
    {
        // Arrange
        const string orderId = "order-12345";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/portfolio/orders/{orderId}/amend")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "old_order": {
                        "order_id": "order-12345",
                        "user_id": "user-abc",
                        "ticker": "MARKET-ABC",
                        "side": "yes",
                        "type": "limit",
                        "status": "resting",
                        "action": "buy",
                        "fill_count_fp": "0.00",
                        "initial_count_fp": "10.00",
                        "remaining_count_fp": "10.00",
                        "yes_price_dollars": "0.55",
                        "no_price_dollars": "0.45",
                        "taker_fill_cost_dollars": "0.00",
                        "maker_fill_cost_dollars": "0.00",
                        "taker_fees_dollars": "0.00",
                        "maker_fees_dollars": "0.00",
                        "queue_position": 3,
                        "created_time": "2024-01-01T00:00:00Z",
                        "outcome_side": "yes",
                        "book_side": "bid"
                    },
                    "order": {
                        "order_id": "order-12345",
                        "user_id": "user-abc",
                        "ticker": "MARKET-ABC",
                        "side": "yes",
                        "type": "limit",
                        "status": "resting",
                        "action": "buy",
                        "fill_count_fp": "0.00",
                        "initial_count_fp": "15.00",
                        "remaining_count_fp": "15.00",
                        "yes_price_dollars": "0.60",
                        "no_price_dollars": "0.40",
                        "taker_fill_cost_dollars": "0.00",
                        "maker_fill_cost_dollars": "0.00",
                        "taker_fees_dollars": "0.00",
                        "maker_fees_dollars": "0.00",
                        "queue_position": 1,
                        "created_time": "2024-01-01T00:00:00Z",
                        "last_update_time": "2024-01-01T00:05:00Z",
                        "cancel_order_on_pause": true,
                        "outcome_side": "yes",
                        "book_side": "bid"
                    }
                }
                """));

        var request = new AmendOrderRequest
        {
            Ticker = "MARKET-ABC",
            Side = OrderSide.Yes,
            Action = "buy",
            YesPriceDollars = "0.60",
            NoPriceDollars = null,
            CountFp = "15.00"
        };

        // Act
        var result = await _client.AmendOrderAsync(orderId, request);

        // Assert
        result.Should().NotBeNull();
        result.OldOrder.Should().NotBeNull();
        result.Order.Should().NotBeNull();

        result.OldOrder.OrderId.Should().Be("order-12345");
        result.OldOrder.UserId.Should().Be("user-abc");
        result.OldOrder.Ticker.Should().Be("MARKET-ABC");
        result.OldOrder.Side.Should().Be(OrderSide.Yes);
        result.OldOrder.Type.Should().Be(OrderType.Limit);
        result.OldOrder.Status.Should().Be(OrderStatus.Resting);
        result.OldOrder.Action.Should().Be("buy");
        result.OldOrder.FillCountFp.Should().Be("0.00");
        result.OldOrder.InitialCountFp.Should().Be("10.00");
        result.OldOrder.RemainingCountFp.Should().Be("10.00");
        result.OldOrder.YesPriceDollars.Should().Be("0.55");
        result.OldOrder.NoPriceDollars.Should().Be("0.45");
        result.OldOrder.TakerFillCostDollars.Should().Be("0.00");
        result.OldOrder.MakerFillCostDollars.Should().Be("0.00");
        result.OldOrder.TakerFeesDollars.Should().Be("0.00");
        result.OldOrder.MakerFeesDollars.Should().Be("0.00");
        result.OldOrder.QueuePosition.Should().Be(3);
        result.OldOrder.CreatedTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        result.OldOrder.OutcomeSide.Should().Be(OrderSide.Yes);
        result.OldOrder.BookSide.Should().Be(OrderBookSide.Bid);

        result.Order.OrderId.Should().Be("order-12345");
        result.Order.UserId.Should().Be("user-abc");
        result.Order.FillCountFp.Should().Be("0.00");
        result.Order.InitialCountFp.Should().Be("15.00");
        result.Order.RemainingCountFp.Should().Be("15.00");
        result.Order.YesPriceDollars.Should().Be("0.60");
        result.Order.NoPriceDollars.Should().Be("0.40");
        result.Order.TakerFillCostDollars.Should().Be("0.00");
        result.Order.MakerFillCostDollars.Should().Be("0.00");
        result.Order.TakerFeesDollars.Should().Be("0.00");
        result.Order.MakerFeesDollars.Should().Be("0.00");
        result.Order.QueuePosition.Should().Be(1);
        result.Order.CreatedTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        result.Order.LastUpdateTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 5, 0, TimeSpan.Zero));
        result.Order.CancelOrderOnPause.Should().BeTrue();
        result.Order.OutcomeSide.Should().Be(OrderSide.Yes);
        result.Order.BookSide.Should().Be(OrderBookSide.Bid);
    }

    [Fact]
    public async Task AmendOrderAsync_WithEmptyOrderId_ThrowsArgumentException()
    {
        // Arrange
        var request = new AmendOrderRequest 
        { 
            Ticker = "MARKET-ABC",
            Side = OrderSide.No,
            Action = "sell",
            NoPriceDollars = "0.60"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.AmendOrderAsync("", request));
        await Assert.ThrowsAsync<ArgumentException>(() => _client.AmendOrderAsync("   ", request));
    }

    [Fact]
    public async Task AmendOrderAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.AmendOrderAsync("order-123", null!));
    }

    [Fact]
    public async Task CancelOrderAsync_ReturnsCancelledOrder()
    {
        // Arrange
        const string orderId = "order-12345";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/portfolio/orders/{orderId}")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "order": {
                        "order_id": "order-12345",
                        "user_id": "user-abc",
                        "ticker": "MARKET-ABC",
                        "side": "yes",
                        "type": "limit",
                        "status": "canceled",
                        "action": "buy",
                        "fill_count_fp": "0.00",
                        "initial_count_fp": "10.00",
                        "remaining_count_fp": "10.00",
                        "yes_price_dollars": "0.55",
                        "no_price_dollars": "0.45",
                        "taker_fill_cost_dollars": "0.00",
                        "maker_fill_cost_dollars": "0.00",
                        "taker_fees_dollars": "0.00",
                        "maker_fees_dollars": "0.00",
                        "created_time": "2024-01-01T00:00:00Z",
                        "last_update_time": "2024-01-01T00:10:00Z",
                        "outcome_side": "yes",
                        "book_side": "bid"
                    }
                }
                """));

        // Act
        var result = await _client.CancelOrderAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be("order-12345");
        result.UserId.Should().Be("user-abc");
        result.Ticker.Should().Be("MARKET-ABC");
        result.Side.Should().Be(OrderSide.Yes);
        result.Type.Should().Be(OrderType.Limit);
        result.Status.Should().Be(OrderStatus.Canceled);
        result.Action.Should().Be("buy");
        result.FillCountFp.Should().Be("0.00");
        result.InitialCountFp.Should().Be("10.00");
        result.RemainingCountFp.Should().Be("10.00");
        result.YesPriceDollars.Should().Be("0.55");
        result.NoPriceDollars.Should().Be("0.45");
        result.TakerFillCostDollars.Should().Be("0.00");
        result.MakerFillCostDollars.Should().Be("0.00");
        result.TakerFeesDollars.Should().Be("0.00");
        result.MakerFeesDollars.Should().Be("0.00");
        result.CreatedTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        result.LastUpdateTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 10, 0, TimeSpan.Zero));
        result.OutcomeSide.Should().Be(OrderSide.Yes);
        result.BookSide.Should().Be(OrderBookSide.Bid);
    }

    [Fact]
    public async Task CancelOrderAsync_WithEmptyOrderId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.CancelOrderAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _client.CancelOrderAsync("   "));
    }

    [Fact]
    public async Task CancelOrderAsync_WithNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders/nonexistent-order")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"not_found","message":"Order not found"}"""));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiNotFoundException>(
            () => _client.CancelOrderAsync("nonexistent-order"));

        exception.ErrorCode.Should().Be("not_found");
    }

    [Fact]
    public async Task GetOrderAsync_ReturnsOrder()
    {
        // Arrange
        const string orderId = "order-12345";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/portfolio/orders/{orderId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "order": {
                        "order_id": "order-12345",
                        "user_id": "user-abc",
                        "client_order_id": "client-order-abc",
                        "ticker": "MARKET-ABC",
                        "side": "no",
                        "type": "market",
                        "status": "executed",
                        "action": "sell",
                        "fill_count_fp": "5.00",
                        "initial_count_fp": "5.00",
                        "remaining_count_fp": "0.00",
                        "yes_price_dollars": "0.45",
                        "no_price_dollars": "0.55",
                        "taker_fill_cost_dollars": "2.75",
                        "maker_fill_cost_dollars": "0.00",
                        "taker_fees_dollars": "0.03",
                        "maker_fees_dollars": "0.00",
                        "created_time": "2024-01-01T00:00:00Z",
                        "last_update_time": "2024-01-01T00:01:00Z",
                        "self_trade_prevention_type": "cancel_resting",
                        "order_group_id": "group-abc",
                        "cancel_order_on_pause": true,
                        "outcome_side": "no",
                        "book_side": "ask"
                    }
                }
                """));

        // Act
        var result = await _client.GetOrderAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be("order-12345");
        result.UserId.Should().Be("user-abc");
        result.ClientOrderId.Should().Be("client-order-abc");
        result.Ticker.Should().Be("MARKET-ABC");
        result.Side.Should().Be(OrderSide.No);
        result.Type.Should().Be(OrderType.Market);
        result.Status.Should().Be(OrderStatus.Executed);
        result.Action.Should().Be("sell");
        result.FillCountFp.Should().Be("5.00");
        result.InitialCountFp.Should().Be("5.00");
        result.RemainingCountFp.Should().Be("0.00");
        result.YesPriceDollars.Should().Be("0.45");
        result.NoPriceDollars.Should().Be("0.55");
        result.TakerFillCostDollars.Should().Be("2.75");
        result.MakerFillCostDollars.Should().Be("0.00");
        result.TakerFeesDollars.Should().Be("0.03");
        result.MakerFeesDollars.Should().Be("0.00");
        result.CreatedTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        result.LastUpdateTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 1, 0, TimeSpan.Zero));
        result.SelfTradePreventionType.Should().Be("cancel_resting");
        result.OrderGroupId.Should().Be("group-abc");
        result.CancelOrderOnPause.Should().BeTrue();
        result.OutcomeSide.Should().Be(OrderSide.No);
        result.BookSide.Should().Be(OrderBookSide.Ask);
    }

    [Fact]
    public async Task GetOrderAsync_WithEmptyOrderId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetOrderAsync(""));
    }

    [Fact]
    public async Task GetOrderAsync_WithNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders/invalid-order")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"not_found","message":"Order not found"}"""));

        // Act & Assert
        await Assert.ThrowsAsync<KalshiNotFoundException>(
            () => _client.GetOrderAsync("invalid-order"));
    }

    [Fact]
    public async Task ListOrdersAsync_WithNoParameters_ReturnsOrders()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "orders": [
                        {
                            "order_id": "order-001",
                            "user_id": "user-abc",
                            "ticker": "MARKET-1",
                            "side": "yes",
                            "type": "limit",
                            "status": "resting",
                            "action": "buy",
                            "fill_count_fp": "5.00",
                            "initial_count_fp": "10.00",
                            "remaining_count_fp": "5.00",
                            "yes_price_dollars": "0.50",
                            "no_price_dollars": "0.50",
                            "taker_fill_cost_dollars": "2.50",
                            "maker_fill_cost_dollars": "0.00",
                            "taker_fees_dollars": "0.03",
                            "maker_fees_dollars": "0.00",
                            "queue_position": 2,
                            "created_time": "2024-01-01T00:00:00Z",
                            "self_trade_prevention_type": "cancel_resting",
                            "cancel_order_on_pause": false,
                            "outcome_side": "yes",
                            "book_side": "bid"
                        },
                        {
                            "order_id": "order-002",
                            "user_id": "user-abc",
                            "ticker": "MARKET-2",
                            "side": "no",
                            "type": "limit",
                            "status": "resting",
                            "action": "buy",
                            "fill_count_fp": "0.00",
                            "initial_count_fp": "20.00",
                            "remaining_count_fp": "20.00",
                            "yes_price_dollars": "0.40",
                            "no_price_dollars": "0.60",
                            "taker_fill_cost_dollars": "0.00",
                            "maker_fill_cost_dollars": "0.00",
                            "taker_fees_dollars": "0.00",
                            "maker_fees_dollars": "0.00",
                            "queue_position": 5,
                            "created_time": "2024-01-01T00:01:40Z",
                            "outcome_side": "no",
                            "book_side": "ask"
                        }
                    ],
                    "cursor": "next-page-cursor"
                }
                """));

        // Act
        var result = await _client.ListOrdersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Cursor.Should().Be("next-page-cursor");
        result.HasMore.Should().BeTrue();

        var first = result.Items[0];
        first.OrderId.Should().Be("order-001");
        first.UserId.Should().Be("user-abc");
        first.Ticker.Should().Be("MARKET-1");
        first.Side.Should().Be(OrderSide.Yes);
        first.Type.Should().Be(OrderType.Limit);
        first.Status.Should().Be(OrderStatus.Resting);
        first.Action.Should().Be("buy");
        first.FillCountFp.Should().Be("5.00");
        first.InitialCountFp.Should().Be("10.00");
        first.RemainingCountFp.Should().Be("5.00");
        first.YesPriceDollars.Should().Be("0.50");
        first.NoPriceDollars.Should().Be("0.50");
        first.TakerFillCostDollars.Should().Be("2.50");
        first.MakerFillCostDollars.Should().Be("0.00");
        first.TakerFeesDollars.Should().Be("0.03");
        first.MakerFeesDollars.Should().Be("0.00");
        first.QueuePosition.Should().Be(2);
        first.CreatedTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        first.SelfTradePreventionType.Should().Be("cancel_resting");
        first.CancelOrderOnPause.Should().BeFalse();
        first.OutcomeSide.Should().Be(OrderSide.Yes);
        first.BookSide.Should().Be(OrderBookSide.Bid);

        var second = result.Items[1];
        second.OrderId.Should().Be("order-002");
        second.FillCountFp.Should().Be("0.00");
        second.QueuePosition.Should().Be(5);
        second.OutcomeSide.Should().Be(OrderSide.No);
        second.BookSide.Should().Be(OrderBookSide.Ask);
    }

    [Fact]
    public async Task ListOrdersAsync_WithQuery_AppliesFilters()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .WithParam("status", "resting")
                .WithParam("ticker", "MARKET-XYZ")
                .WithParam("limit", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "orders": [],
                    "cursor": null
                }
                """));

        var query = new OrderQuery
        {
            Status = OrderStatus.Resting,
            Ticker = "MARKET-XYZ",
            Limit = 50
        };

        // Act
        var result = await _client.ListOrdersAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ListOrdersAsync_WithCursor_FetchesNextPage()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .WithParam("cursor", "page-2-cursor")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "orders": [
                        {
                            "order_id": "order-003",
                            "user_id": "user-abc",
                            "ticker": "MARKET-3",
                            "side": "yes",
                            "type": "limit",
                            "status": "canceled",
                            "action": "buy",
                            "fill_count_fp": "0.00",
                            "initial_count_fp": "5.00",
                            "remaining_count_fp": "5.00",
                            "yes_price_dollars": "0.70",
                            "no_price_dollars": "0.30",
                            "taker_fill_cost_dollars": "0.00",
                            "maker_fill_cost_dollars": "0.00",
                            "taker_fees_dollars": "0.00",
                            "maker_fees_dollars": "0.00",
                            "created_time": "2024-01-01T00:03:20Z",
                            "last_update_time": "2024-01-01T00:04:00Z",
                            "outcome_side": "yes",
                            "book_side": "bid"
                        }
                    ],
                    "cursor": null
                }
                """));

        var query = new OrderQuery { Cursor = "page-2-cursor" };

        // Act
        var result = await _client.ListOrdersAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].OrderId.Should().Be("order-003");
        result.Items[0].Status.Should().Be(OrderStatus.Canceled);
        result.Items[0].CreatedTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 3, 20, TimeSpan.Zero));
        result.Items[0].LastUpdateTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 4, 0, TimeSpan.Zero));
        result.Items[0].OutcomeSide.Should().Be(OrderSide.Yes);
        result.Items[0].BookSide.Should().Be(OrderBookSide.Bid);
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ListOrdersAsync_WithEventTickerFilter_AppliesFilter()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .WithParam("event_ticker", "EVENT-123")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "orders": [],
                    "cursor": null
                }
                """));

        var query = new OrderQuery { EventTicker = "EVENT-123" };

        // Act
        var result = await _client.ListOrdersAsync(query);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateOrderAsync_WithClientOrderId_IncludesInRequest()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .WithBody(body => body != null && body.Contains("\"client_order_id\"") && body.Contains("\"my-custom-id\""))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "order": {
                        "order_id": "order-99999",
                        "client_order_id": "my-custom-id",
                        "ticker": "MARKET-TEST",
                        "side": "yes",
                        "type": "limit",
                        "status": "resting",
                        "action": "buy",
                        "fill_count_fp": "0.00",
                        "initial_count_fp": "1.00",
                        "remaining_count_fp": "1.00",
                        "yes_price_dollars": "0.50",
                        "no_price_dollars": "0.50"
                    }
                }
                """));

        var request = new CreateOrderRequest
        {
            Ticker = "MARKET-TEST",
            Side = OrderSide.Yes,
            Action = "buy",
            CountFp = "1.00",
            Type = OrderType.Limit,
            YesPriceDollars = "0.50",
            ClientOrderId = "my-custom-id"
        };

        // Act
        var result = await _client.CreateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ClientOrderId.Should().Be("my-custom-id");
    }

    [Fact]
    public async Task CreateOrderAsync_WithAuthError_ThrowsAuthException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"unauthorized","message":"Invalid API key"}"""));

        var request = new CreateOrderRequest
        {
            Ticker = "MARKET-ABC",
            Side = OrderSide.Yes,
            Action = "buy",
            CountFp = "10.00",
            Type = OrderType.Limit,
            YesPriceDollars = "0.55"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiAuthException>(
            () => _client.CreateOrderAsync(request));

        exception.ErrorCode.Should().Be("unauthorized");
    }

    [Fact]
    public async Task CreateOrderAsync_ServerError_ThrowsKalshiException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"internal_error","message":"Internal server error"}"""));

        var request = new CreateOrderRequest
        {
            Ticker = "MARKET-ABC",
            Side = OrderSide.Yes,
            Action = "buy",
            CountFp = "10.00",
            Type = OrderType.Limit,
            YesPriceDollars = "0.55"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KalshiException>(() => _client.CreateOrderAsync(request));
    }

    [Fact]
    public async Task GetOrderAsync_RestingOrder_HasQueuePosition()
    {
        // Arrange
        const string orderId = "order-resting";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/portfolio/orders/{orderId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "order": {
                        "order_id": "order-resting",
                        "ticker": "MARKET-ABC",
                        "side": "yes",
                        "type": "limit",
                        "status": "resting",
                        "action": "buy",
                        "queue_position": 7
                    }
                }
                """));

        // Act
        var result = await _client.GetOrderAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Resting);
        result.QueuePosition.Should().Be(7);
    }

    [Fact]
    public async Task GetOrderAsync_WithOrderGroupId_ReturnsGroupId()
    {
        // Arrange
        const string orderId = "order-grouped";
        _server.Given(Request.Create()
                .WithPath($"/trade-api/v2/portfolio/orders/{orderId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "order": {
                        "order_id": "order-grouped",
                        "ticker": "MARKET-ABC",
                        "side": "yes",
                        "type": "limit",
                        "status": "resting",
                        "action": "buy",
                        "order_group_id": "batch-group-001"
                    }
                }
                """));

        // Act
        var result = await _client.GetOrderAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.OrderGroupId.Should().Be("batch-group-001");
    }

    [Fact]
    public async Task ListOrdersAsync_WithMinTsFilter_IncludesQueryParam()
    {
        // Arrange
        var minTs = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expectedParam = minTs.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .WithParam("min_ts", expectedParam)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"orders": [], "cursor": null}"""));

        var query = new OrderQuery { MinTs = minTs };

        // Act
        var result = await _client.ListOrdersAsync(query);

        // Assert
        result.Should().NotBeNull();
        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListOrdersAsync_WithMaxTsFilter_IncludesQueryParam()
    {
        // Arrange
        var maxTs = new DateTimeOffset(2024, 6, 30, 23, 59, 59, TimeSpan.Zero);
        var expectedParam = maxTs.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .WithParam("max_ts", expectedParam)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"orders": [], "cursor": null}"""));

        var query = new OrderQuery { MaxTs = maxTs };

        // Act
        var result = await _client.ListOrdersAsync(query);

        // Assert
        result.Should().NotBeNull();
        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListOrdersAsync_WithAllFilters_IncludesAllQueryParams()
    {
        // Arrange
        var minTs = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var maxTs = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);

        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .WithParam("status", "resting")
                .WithParam("ticker", "MARKET-XYZ")
                .WithParam("event_ticker", "EVENT-123")
                .WithParam("limit", "25")
                .WithParam("cursor", "some-cursor")
                .WithParam("min_ts", minTs.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
                .WithParam("max_ts", maxTs.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"orders": [], "cursor": null}"""));

        var query = new OrderQuery
        {
            Status = OrderStatus.Resting,
            Ticker = "MARKET-XYZ",
            EventTicker = "EVENT-123",
            Limit = 25,
            Cursor = "some-cursor",
            MinTs = minTs,
            MaxTs = maxTs
        };

        // Act
        var result = await _client.ListOrdersAsync(query);

        // Assert
        result.Should().NotBeNull();
        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListOrdersAsync_NullCursor_HasMoreIsFalse()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "orders": [
                        {
                            "order_id": "order-last",
                            "ticker": "MARKET-1",
                            "side": "yes",
                            "type": "limit",
                            "status": "resting",
                            "action": "buy"
                        }
                    ],
                    "cursor": null
                }
                """));

        // Act
        var result = await _client.ListOrdersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Cursor.Should().BeNull();
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ListOrdersAsync_Unauthorized_ThrowsAuthException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"unauthorized","message":"Invalid API key"}"""));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiAuthException>(
            () => _client.ListOrdersAsync());

        exception.ErrorCode.Should().Be("unauthorized");
    }
}
