using FluentAssertions;
using KalshiSharp.Core.Auth;
using KalshiSharp.Core.Configuration;
using KalshiSharp.Core.Errors;
using KalshiSharp.Core.Http;
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
                    "order_id": "order-12345",
                    "ticker": "MARKET-ABC",
                    "side": "yes",
                    "type": "limit",
                    "status": "resting",
                    "action": "buy",
                    "count": 10,
                    "remaining_count": 10,
                    "yes_price": 55,
                    "no_price": 45,
                    "time_in_force": "gtc",
                    "created_time": 1704067200000
                }
                """));

        var request = new CreateOrderRequest
        {
            Ticker = "MARKET-ABC",
            Side = OrderSide.Yes,
            Action = "buy",
            Count = 10,
            Type = OrderType.Limit,
            YesPrice = 55
        };

        // Act
        var result = await _client.CreateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be("order-12345");
        result.Ticker.Should().Be("MARKET-ABC");
        result.Side.Should().Be(OrderSide.Yes);
        result.Type.Should().Be(OrderType.Limit);
        result.Status.Should().Be(OrderStatus.Resting);
        result.Action.Should().Be("buy");
        result.Count.Should().Be(10);
        result.RemainingCount.Should().Be(10);
        result.YesPrice.Should().Be(55);
        result.NoPrice.Should().Be(45);
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
            Count = -1,
            Type = OrderType.Limit,
            YesPrice = 55
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
                .WithPath($"/trade-api/v2/portfolio/orders/{orderId}")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "order_id": "order-12345",
                    "ticker": "MARKET-ABC",
                    "side": "yes",
                    "type": "limit",
                    "status": "resting",
                    "action": "buy",
                    "count": 15,
                    "remaining_count": 15,
                    "yes_price": 60,
                    "no_price": 40,
                    "time_in_force": "gtc",
                    "created_time": 1704067200000,
                    "updated_time": 1704067300000
                }
                """));

        var request = new AmendOrderRequest
        {
            Price = 60,
            Count = 15
        };

        // Act
        var result = await _client.AmendOrderAsync(orderId, request);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be("order-12345");
        result.Count.Should().Be(15);
        result.YesPrice.Should().Be(60);
        result.UpdatedTime.Should().NotBeNull();
    }

    [Fact]
    public async Task AmendOrderAsync_WithEmptyOrderId_ThrowsArgumentException()
    {
        // Arrange
        var request = new AmendOrderRequest { Price = 60 };

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
                    "order_id": "order-12345",
                    "ticker": "MARKET-ABC",
                    "side": "yes",
                    "type": "limit",
                    "status": "canceled",
                    "action": "buy",
                    "count": 10,
                    "remaining_count": 10,
                    "yes_price": 55,
                    "no_price": 45,
                    "time_in_force": "gtc",
                    "created_time": 1704067200000,
                    "decrease_reason": "user_cancelled"
                }
                """));

        // Act
        var result = await _client.CancelOrderAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be("order-12345");
        result.Status.Should().Be(OrderStatus.Canceled);
        result.DecreaseReason.Should().Be("user_cancelled");
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
                    "order_id": "order-12345",
                    "client_order_id": "client-order-abc",
                    "ticker": "MARKET-ABC",
                    "side": "no",
                    "type": "market",
                    "status": "executed",
                    "action": "sell",
                    "count": 5,
                    "remaining_count": 0,
                    "yes_price": 45,
                    "no_price": 55,
                    "time_in_force": "ioc",
                    "created_time": 1704067200000,
                    "fees_paid": 10
                }
                """));

        // Act
        var result = await _client.GetOrderAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be("order-12345");
        result.ClientOrderId.Should().Be("client-order-abc");
        result.Ticker.Should().Be("MARKET-ABC");
        result.Side.Should().Be(OrderSide.No);
        result.Type.Should().Be(OrderType.Market);
        result.Status.Should().Be(OrderStatus.Executed);
        result.Action.Should().Be("sell");
        result.Count.Should().Be(5);
        result.RemainingCount.Should().Be(0);
        result.FilledCount.Should().Be(5);
        result.TimeInForce.Should().Be(TimeInForce.Ioc);
        result.FeesPaid.Should().Be(10);
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
                    "items": [
                        {
                            "order_id": "order-001",
                            "ticker": "MARKET-1",
                            "side": "yes",
                            "type": "limit",
                            "status": "resting",
                            "action": "buy",
                            "count": 10,
                            "remaining_count": 5,
                            "yes_price": 50,
                            "no_price": 50,
                            "time_in_force": "gtc",
                            "created_time": 1704067200000
                        },
                        {
                            "order_id": "order-002",
                            "ticker": "MARKET-2",
                            "side": "no",
                            "type": "limit",
                            "status": "resting",
                            "action": "buy",
                            "count": 20,
                            "remaining_count": 20,
                            "yes_price": 40,
                            "no_price": 60,
                            "time_in_force": "gtc",
                            "created_time": 1704067300000
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
        result.Items[0].OrderId.Should().Be("order-001");
        result.Items[0].FilledCount.Should().Be(5);
        result.Items[1].OrderId.Should().Be("order-002");
        result.Cursor.Should().Be("next-page-cursor");
        result.HasMore.Should().BeTrue();
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
                    "items": [],
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
                    "items": [
                        {
                            "order_id": "order-003",
                            "ticker": "MARKET-3",
                            "side": "yes",
                            "type": "limit",
                            "status": "canceled",
                            "action": "buy",
                            "count": 5,
                            "remaining_count": 5,
                            "yes_price": 70,
                            "no_price": 30,
                            "time_in_force": "gtc",
                            "created_time": 1704067400000
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
                    "items": [],
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
                    "order_id": "order-99999",
                    "client_order_id": "my-custom-id",
                    "ticker": "MARKET-TEST",
                    "side": "yes",
                    "type": "limit",
                    "status": "resting",
                    "action": "buy",
                    "count": 1,
                    "remaining_count": 1,
                    "yes_price": 50,
                    "no_price": 50,
                    "time_in_force": "gtc",
                    "created_time": 1704067200000
                }
                """));

        var request = new CreateOrderRequest
        {
            Ticker = "MARKET-TEST",
            Side = OrderSide.Yes,
            Action = "buy",
            Count = 1,
            Type = OrderType.Limit,
            YesPrice = 50,
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
            Count = 10,
            Type = OrderType.Limit,
            YesPrice = 55
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiAuthException>(
            () => _client.CreateOrderAsync(request));

        exception.ErrorCode.Should().Be("unauthorized");
    }
}
