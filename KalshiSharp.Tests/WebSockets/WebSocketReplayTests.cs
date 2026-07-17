using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Serialization;
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.WebSocket;
using KalshiSharp.WebSockets;
using KalshiSharp.WebSockets.Connections;
using KalshiSharp.WebSockets.ReconnectPolicy;
using KalshiSharp.WebSockets.Subscriptions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using System.Globalization;

namespace KalshiSharp.Tests.WebSockets;

/// <summary>
/// WebSocket message replay tests to verify message parsing and dispatch.
/// </summary>
public sealed class WebSocketReplayTests : IAsyncDisposable
{
    // Test RSA private key for signing (not a real key - only used for unit tests)
    private const string TestRsaPrivateKey = """
        -----BEGIN PRIVATE KEY-----
        MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC9nETfOUf/rPoF
        D53iaRwJ9cOHf6ewQZ3t/M3NQPgF/MYFU7txHzBjo8AuS+BMkLfZpxssl1YTZ6wW
        jlCEzmNrc4yDXZX9JIF9wMHdla+zOVVKEtsI3Pp9rvDOQof8J3C1+HugVA3Uqlqf
        Ot6i5T74XBeu4jLpGRF+uLcLtVHEY/LFPZd5pyJTckKi52/eJCKM5mH8szIysxil
        ewH04nU0p2J9Wp0qMPbC2uCA+8pb++94QUmQIZb4LoCdeT8r66OKz2k+csNITGxo
        gCfj6uDJ119ckGXWqa+2zvbscVIf75pYqC06/8YB6J7I79Y3Z4NCTWHMJ36oqnoQ
        HdzRMJlrAgMBAAECggEAHmohMA9fqbcE+efZ4xYKLdzSyvrimqbD3wd0ua5ouokj
        +HnIcOpYWDtNmf+I0K9MFmk0NjBmWcGA/LNCXjM/Bl7oFLBf3VXMQbA4SMN4hg61
        zCZ/JQpRUfTMYsGQT5XCAiaEKiEhgNH8rFsEmGuecLdRAzf8g6CGSmX10rZ4kcBR
        ndIiRJ3INWlDtwSTB1/VVi7gVOwbpzkMvLWvAoeulPWVRDT9vzcJ14/ZHfMAexSj
        s804lsx/b/Btwh9X0RstXv9VDT13a8ADb0+VKCaxda944AxySGYriD4XaSwIviRJ
        IA9CnUrplj+VpV4V6bpxWB/1bC1wctr8lp+9szf4qQKBgQDwlI6prST/jDMo8nyr
        RHbMSYQICRpy88+BjNWCB31albpSow5Q5xJCo5m+mbIp7gHGM1xjAC0CJCYI66pU
        HyY6zjCaDhD6ZFtSidLfbiEQFfSsmH+Zh66DO3P3Zl/EEuA/l5JJRK02KnExcbqq
        uf4YQQQqfu4y15ClWqHOcrK8MwKBgQDJw2XVlkA+Y4OAfwlSjJQ/0o5tlYnnFu9H
        tvkCaYACmzhzOdxtFWeDu3e2h0bv/2RabMv0Z+ss/jbEm+a2JYAtU3B0ROkE+T07
        o1rmsYcAwIafP02VrbxcCxoiCUQfsiEKKkntJ13gVUn27i8pCO19NNsn+x/hvqGq
        4bld0YB16QKBgAb6eCzpzdHv0igU6JLbOIrycvb8tJyy/8jlOeg8qWEwSKhO/IJS
        QZBXSIVj1ewrcDe8k6h3f9a5D7VgiJ9KDATWqEg/sjRhJtj9EHXUrvbVfDRpdAIT
        EnfSCKobeRmp5oFRtzeS22df0cq6XszG+lzfvewxpF0rLZHuUBU59H9LAoGBAI0X
        A+5RTImUQ1AnBdjhD4Z18j11deLQqfEnZYgnSGoKK3aAPsFVV3bKMJPGk3eey4lk
        TVeTF+T1vEzOjI5ROQn5MElOKvjcZdJ/kECEYljHSRyxQsrpnC9tYA/vFOFpSit2
        mQ2rGr2WRsvTkc0LPi/xN1QFCy1shlcd0+dkaoWJAoGAH7JB8B4dkF4wiMw8REeU
        VGgsrK4Az4DpVrMwvRAgiclQ2BWRKKYomYFRQTaxZQiK6e6+U6Wx+uCtD6xfGqPL
        WSbtzPKf+c7URfAI+hggsvDmHlLtCNrv0uPPv3g0Qzw3l8hFgJBTT9bb3jgdAxc1
        +3EvzhRBF16l2qi0IEdPwM0=
        -----END PRIVATE KEY-----
        """;

    private readonly MockWebSocketConnection _mockConnection;
    private readonly KalshiWebSocketClient _client;

    public WebSocketReplayTests()
    {
        var options = Options.Create(new KalshiClientOptions
        {
            ApiKey = "test-api-key",
            ApiSecret = TestRsaPrivateKey,
            Environment = KalshiEnvironment.Demo
        });

        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _mockConnection = new MockWebSocketConnection();
        var reconnectPolicy = new ExponentialBackoffPolicy();

        _client = new KalshiWebSocketClient(
            options,
            _mockConnection,
            reconnectPolicy,
            clock,
            NullLogger<KalshiWebSocketClient>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }

    private async Task<List<WebSocketMessage>> ReceiveOneMessage(string json)
    {
        _mockConnection.EnqueueMessage(json);
        var messages = new List<WebSocketMessage>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        try
        {
            await foreach (var msg in _client.Messages.WithCancellation(cts.Token))
            {
                messages.Add(msg);
                if (messages.Count >= 1) break;
            }
        }
        catch (OperationCanceledException) { }
        return messages;
    }

    #region Connection

    [Fact]
    public void State_Initially_IsDisconnected()
    {
        _client.State.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public async Task ConnectAsync_TransitionsToAuthenticated()
    {
        // Arrange
        _mockConnection.SetupConnect();

        // Act
        await _client.ConnectAsync();

        // Assert
        _client.State.Should().Be(ConnectionState.Authenticated);
    }

    [Fact]
    public async Task DisconnectAsync_TransitionsToDisconnected()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        // Act
        await _client.DisconnectAsync();

        // Assert
        _client.State.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _client.ConnectAsync());
    }

    [Fact]
    public async Task StateChanged_EventRaised_OnStateChange()
    {
        // Arrange
        var stateChanges = new List<(ConnectionState Previous, ConnectionState New)>();
        _client.StateChanged += (_, e) => stateChanges.Add((e.PreviousState, e.NewState));

        _mockConnection.SetupConnect();

        // Act
        await _client.ConnectAsync();

        // Assert
        stateChanges.Should().Contain((ConnectionState.Disconnected, ConnectionState.Connecting));
        stateChanges.Should().Contain((ConnectionState.Connecting, ConnectionState.Connected));
        stateChanges.Should().Contain((ConnectionState.Connected, ConnectionState.Authenticated));
    }

    #endregion

    #region Subscriptions

    [Fact]
    public async Task SubscribeAsync_SendsSubscribeCommand()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var subscription = new OrderBookSubscription { Markets = ["MARKET-ABC"] };

        // Act
        await _client.SubscribeAsync(subscription);

        // Assert
        var sentMessages = _mockConnection.SentMessages;
        sentMessages.Should().HaveCountGreaterOrEqualTo(1); // Subscribe (auth is now via headers)

        var subscribeMessage = sentMessages[^1];
        subscribeMessage.Should().Contain("\"cmd\":\"subscribe\"");
        subscribeMessage.Should().Contain("\"channels\":[\"orderbook_delta\"]");
        subscribeMessage.Should().Contain("\"market_tickers\":[\"MARKET-ABC\"]");
    }

    [Fact]
    public async Task UnsubscribeAsync_SendsUnsubscribeCommand()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var subscription = new TradeSubscription { Markets = ["MARKET-XYZ"] };
        await _client.SubscribeAsync(subscription);

        // Act
        await _client.UnsubscribeAsync(subscription);

        // Assert
        var lastMessage = _mockConnection.SentMessages[^1];
        lastMessage.Should().Contain("\"cmd\":\"unsubscribe\"");
        lastMessage.Should().Contain("\"channels\":[\"trade\"]");
    }

    [Fact]
    public async Task SubscribeAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var subscription = new OrderBookSubscription { Markets = ["MARKET-ABC"] };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _client.SubscribeAsync(subscription));
    }

    #endregion

    #region OrderBookUpdate

    [Fact]
    public async Task Messages_ReceivesOrderBookUpdate()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "orderbook_delta",
                "seq": 12345,
                "msg": {
                    "market_ticker": "MARKET-ABC",
                    "market_id": "6F31765E-D070-41B9-A6EA-6AF3274B362B",
                    "price_dollars": "0.50",
                    "delta_fp": "100.00",
                    "side": "yes",
                    "client_order_id": "client-order-123",
                    "subaccount": "0",
                    "ts": "2024-01-01T00:00:00Z"
                }
            }
            """);

        // Assert
        messages.Should().HaveCount(1);
        var update = messages[0].Should().BeOfType<OrderBookUpdate>().Subject;
        update.Sequence.Should().Be(12345);
        update.Message.MarketTicker.Should().Be("MARKET-ABC");
        update.Message.MarketId.Should().Be("6F31765E-D070-41B9-A6EA-6AF3274B362B");
        update.Message.PriceDollars.Should().Be("0.50");
        update.Message.DeltaFp.Should().Be("100.00");
        update.Message.Side.Should().Be("yes");
        update.Message.ClientOrderId.Should().Be("client-order-123");
        update.Message.IsYesSide.Should().BeTrue();
        update.Message.IsNoSide.Should().BeFalse();
        update.Message.Price().Should().Be(50);
        update.Message.Delta().Should().Be(100m);
    }

    #endregion

    #region OrderBookSnapshot

    [Fact]
    public async Task Messages_ReceivesOrderBookSnapshot()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "orderbook_snapshot",
                "msg": {
                    "market_ticker": "MARKET-ABC",
                    "market_id": "6F31765E-D070-41B9-A6EA-6AF3274B362B",
                    "yes_dollars": [[0.50, 100], [0.51, 200]],
                    "yes_dollars_fp": [[0.50, 100.00], [0.51, 200.00]],
                    "no_dollars": [[0.49, 150]],
                    "no_dollars_fp": [[0.49, 150.00]]
                }
            }
            """);

        // Act
        var snapshot = messages[0].Should().BeOfType<OrderBookSnapshot>().Subject;

        // Assert
        snapshot.Message.MarketTicker.Should().Be("MARKET-ABC");
        snapshot.Message.MarketId.Should().Be("6F31765E-D070-41B9-A6EA-6AF3274B362B");
        snapshot.Message.YesDollars.Should().HaveCount(2);
        snapshot.Message.YesDollarsFp.Should().HaveCount(2);
        snapshot.Message.NoDollars.Should().HaveCount(1);
        snapshot.Message.NoDollarsFp.Should().HaveCount(1);
    }

    #endregion

    #region TradeUpdate

    [Fact]
    public async Task Messages_ReceivesTradeUpdate()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "trade",
                "seq": 999,
                "msg": {
                    "trade_id": "trade-123",
                    "market_ticker": "MARKET-XYZ",
                    "market_id": "6F31765E-D070-41B9-A6EA-6AF3274B362B",
                    "side": "yes",
                    "yes_price_dollars": "0.65",
                    "no_price_dollars": "0.35",
                    "count_fp": "50.00",
                    "taker_side": "yes",
                    "ts": 1704067200000
                }
            }
            """);

        // Act
        var trade = messages[0].Should().BeOfType<TradeUpdate>().Subject;

        // Assert
        trade.Message.TradeId.Should().Be("trade-123");
        trade.Message.MarketTicker.Should().Be("MARKET-XYZ");
        trade.Message.Side.Should().Be(OrderSide.Yes);
        trade.Message.YesPriceDollars.Should().Be("0.65");
        trade.Message.NoPriceDollars.Should().Be("0.35");
        trade.Message.CountFp.Should().Be("50.00");
        trade.Message.TakerSide.Should().Be("yes");
        trade.Message.TimeStampMs.Should().Be(1704067200000);
        trade.Message.TimeStamp.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1704067200000));
        trade.Message.YesPrice().Should().Be(65);
        trade.Message.NoPrice().Should().Be(35);
        trade.Message.Count().Should().Be(50m);
    }

    #endregion

    #region TickerUpdate

    [Fact]
    public async Task Messages_ReceivesTickerUpdate()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "ticker",
                "seq": 1234,
                "msg": {
                    "market_ticker": "MARKET-ABC",
                    "market_id": "6F31765E-D070-41B9-A6EA-6AF3274B362B",
                    "price_dollars": "0.55",
                    "yes_bid_dollars": "0.54",
                    "yes_ask_dollars": "0.56",
                    "volume_fp": "10000.00",
                    "open_interest_fp": "5000.00",
                    "ts": 1771526292,
                    "time": "2026-02-19T18:38:12.398904Z",
                    "dollar_volume": 5500,
                    "dollar_open_interest": 2750
                }
            }
            """);

        // Act
        var ticker = messages[0].Should().BeOfType<TickerUpdate>().Subject;

        // Assert
        ticker.Message.MarketTicker.Should().Be("MARKET-ABC");
        ticker.Message.MarketId.Should().Be(Guid.Parse("6F31765E-D070-41B9-A6EA-6AF3274B362B"));
        ticker.Message.PriceDollars.Should().Be("0.55");
        ticker.Message.YesBidDollars.Should().Be("0.54");
        ticker.Message.YesAskDollars.Should().Be("0.56");
        ticker.Message.VolumeFp.Should().Be("10000.00");
        ticker.Message.OpenInterestFp.Should().Be("5000.00");
        ticker.Message.TimeStamp.Should().Be(1771526292);
        ticker.Message.Time.Should().Be(DateTimeOffset.Parse("2026-02-19T18:38:12.398904Z", CultureInfo.InvariantCulture));
        ticker.Message.DollarVolume.Should().Be(5500);
        ticker.Message.DollarOpenInterest.Should().Be(2750);
        ticker.Message.Price().Should().Be(55);
        ticker.Message.YesBid().Should().Be(54);
        ticker.Message.YesAsk().Should().Be(56);
        ticker.Message.NoBid().Should().Be(44);
        ticker.Message.NoAsk().Should().Be(46);
        ticker.Message.Volume().Should().Be(10000m);
        ticker.Message.OpenInterest().Should().Be(5000m);
    }

    #endregion

    #region MarketPositionUpdate

    [Fact]
    public async Task Messages_ReceivesMarketPositionUpdate()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "market_position",
                "seq": 5678,
                "msg": {
                    "user_id": "user-123",
                    "market_ticker": "MARKET-ABC",
                    "position_fp": "100.00",
                    "position_cost_dollars": "5.5000",
                    "realized_pnl_dollars": "0.5000",
                    "fees_paid_dollars": "0.0500",
                    "position_fee_cost_dollars": "0.0500",
                    "volume_fp": "200.00",
                    "subaccount": 0
                }
            }
            """);

        // Act
        var pos = messages[0].Should().BeOfType<MarketPositionUpdate>().Subject;

        // Assert
        pos.Message.UserId.Should().Be("user-123");
        pos.Message.MarketTicker.Should().Be("MARKET-ABC");
        pos.Message.PositionFp.Should().Be("100.00");
        pos.Message.PositionCostDollars.Should().Be("5.5000");
        pos.Message.RealizedPnlDollars.Should().Be("0.5000");
        pos.Message.FeesPaidDollars.Should().Be("0.0500");
        pos.Message.PositionFeeCostDollars.Should().Be("0.0500");
        pos.Message.VolumeFp.Should().Be("200.00");
        pos.Message.Subaccount.Should().Be(0);
        pos.Message.Position().Should().Be(100m);
        pos.Message.PositionCostDollars().Should().Be(5.5000m);
        pos.Message.RealizedPnlDollars().Should().Be(0.5000m);
        pos.Message.FeesPaidDollars().Should().Be(0.0500m);
        pos.Message.PositionFeeCostDollars().Should().Be(0.0500m);
        pos.Message.Volume().Should().Be(200m);
    }

    #endregion

    #region FillUpdate

    [Fact]
    public async Task Messages_ReceivesFillUpdate()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "fill",
                "seq": 9999,
                "msg": {
                    "trade_id": "trade-789",
                    "order_id": "order-456",
                    "market_ticker": "MARKET-XYZ",
                    "is_taker": true,
                    "side": "yes",
                    "yes_price_dollars": "0.6500",
                    "count_fp": "10.00",
                    "post_position_fp": "110.00",
                    "fee_cost": "0.0050",
                    "action": "buy",
                    "ts": 1704067200,
                    "client_order_id": "client-order-123",
                    "purchased_side": "yes",
                    "subaccount": 0
                }
            }
            """);

        // Act
        var fill = messages[0].Should().BeOfType<FillUpdate>().Subject;

        // Assert
        fill.Message.TradeId.Should().Be("trade-789");
        fill.Message.OrderId.Should().Be("order-456");
        fill.Message.MarketTicker.Should().Be("MARKET-XYZ");
        fill.Message.IsTaker.Should().BeTrue();
        fill.Message.Side.Should().Be(OrderSide.Yes);
        fill.Message.YesPriceDollars.Should().Be("0.6500");
        fill.Message.CountFp.Should().Be("10.00");
        fill.Message.PostPositionFp.Should().Be("110.00");
        fill.Message.FeeCost.Should().Be("0.0050");
        fill.Message.Action.Should().Be("buy");
        fill.Message.Ts.Should().Be(1704067200);
        fill.Message.ClientOrderId.Should().Be("client-order-123");
        fill.Message.PurchasedSide.Should().Be(OrderSide.Yes);
        fill.Message.Subaccount.Should().Be(0);
        fill.Message.YesPrice().Should().Be(65);
        fill.Message.NoPrice().Should().Be(35);
        fill.Message.Count().Should().Be(10m);
        fill.Message.PostPosition().Should().Be(110m);
        fill.Message.FillPrice().Should().Be(65);
    }

    #endregion

    #region OrderUpdate

    [Fact]
    public async Task Messages_ReceivesOrderUpdate()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "user_order",
                "seq": 100,
                "msg": {
                    "order_id": "order-456",
                    "user_id": "user-123",
                    "ticker": "MARKET-ABC",
                    "status": "resting",
                    "side": "yes",
                    "outcome_side": "yes",
                    "book_side": "bid",
                    "yes_price_dollars": "0.5500",
                    "fill_count_fp": "25.00",
                    "remaining_count_fp": "25.00",
                    "initial_count_fp": "50.00",
                    "taker_fill_cost_dollars": "0.0000",
                    "maker_fill_cost_dollars": "0.0000",
                    "taker_fees_dollars": "0.0010",
                    "maker_fees_dollars": "0.0005",
                    "client_order_id": "my-client-order",
                    "order_group_id": "group-001",
                    "self_trade_prevention_type": "cancel_resting",
                    "created_time": "2024-01-01T00:00:00Z",
                    "last_update_time": "2024-01-01T00:01:00Z",
                    "expiration_time": "2024-01-02T00:00:00Z",
                    "subaccount_number": 1
                }
            }
            """);

        // Act
        var order = messages[0].Should().BeOfType<OrderUpdate>().Subject;

        // Assert
        order.Message.OrderId.Should().Be("order-456");
        order.Message.UserId.Should().Be("user-123");
        order.Message.Ticker.Should().Be("MARKET-ABC");
        order.Message.Status.Should().Be(OrderStatus.Resting);
        order.Message.Side.Should().Be(OrderSide.Yes);
        order.Message.OutcomeSide.Should().Be(OrderSide.Yes);
        order.Message.BookSide.Should().Be(OrderBookSide.Bid);
        order.Message.YesPriceDollars.Should().Be("0.5500");
        order.Message.FillCountFp.Should().Be("25.00");
        order.Message.RemainingCountFp.Should().Be("25.00");
        order.Message.InitialCountFp.Should().Be("50.00");
        order.Message.TakerFillCostDollars.Should().Be("0.0000");
        order.Message.MakerFillCostDollars.Should().Be("0.0000");
        order.Message.TakerFeesDollars.Should().Be("0.0010");
        order.Message.MakerFeesDollars.Should().Be("0.0005");
        order.Message.ClientOrderId.Should().Be("my-client-order");
        order.Message.OrderGroupId.Should().Be("group-001");
        order.Message.SelfTradePreventionType.Should().Be("cancel_resting");
        order.Message.CreatedTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        order.Message.LastUpdateTime.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 1, 0, TimeSpan.Zero));
        order.Message.ExpirationTime.Should().Be(new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero));
        order.Message.SubaccountNumber.Should().Be(1);
        order.Message.NoPriceInDollars.Should().Be("0.4500");
        order.Message.OrderPrice.Should().Be("0.5500");
    }

    [Fact]
    public void MessageParsing_OrderUpdate_ParsesCorrectly()
    {
        // Arrange
        var json = """
            {
                "type": "user_order",
                "seq": 100,
                "msg": {
                    "order_id": "order-456",
                    "user_id": "user-123",
                    "ticker": "MARKET-ABC",
                    "status": "resting",
                    "side": "yes",
                    "outcome_side": "yes",
                    "book_side": "bid",
                    "yes_price_dollars": "0.5500",
                    "fill_count_fp": "25.00",
                    "remaining_count_fp": "25.00",
                    "initial_count_fp": "50.00",
                    "self_trade_prevention_type": "cancel_resting",
                    "created_time": "2024-01-01T00:00:00Z",
                    "last_update_time": "2024-01-01T00:00:00Z"
                }
            }
            """;

        // Act
        var message = JsonSerializer.Deserialize<WebSocketMessage>(json, KalshiJsonOptions.Default);

        // Assert
        var orderUpdate = message.Should().BeOfType<OrderUpdate>().Subject;
        orderUpdate.Message.OrderId.Should().Be("order-456");
        orderUpdate.Message.UserId.Should().Be("user-123");
        orderUpdate.Message.Ticker.Should().Be("MARKET-ABC");
        orderUpdate.Message.Side.Should().Be(OrderSide.Yes);
        orderUpdate.Message.OutcomeSide.Should().Be(OrderSide.Yes);
        orderUpdate.Message.BookSide.Should().Be(OrderBookSide.Bid);
        orderUpdate.Message.Status.Should().Be(OrderStatus.Resting);
        orderUpdate.Message.YesPriceDollars.Should().Be("0.5500");
        orderUpdate.Message.FillCountFp.Should().Be("25.00");
        orderUpdate.Message.RemainingCountFp.Should().Be("25.00");
        orderUpdate.Message.InitialCountFp.Should().Be("50.00");
    }

    #endregion

    #region SubscriptionConfirmation

    [Fact]
    public async Task Messages_ReceivesSubscriptionConfirmation()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "subscribed",
                "msg": {
                    "channel": "orderbook_delta"
                }
            }
            """);

        // Act
        var confirm = messages[0].Should().BeOfType<SubscriptionConfirmation>().Subject;

        // Assert
        confirm.Message.Channel.Should().Be("orderbook_delta");
    }

    #endregion

    #region UnsubscriptionConfirmation

    [Fact]
    public async Task Messages_ReceivesUnsubscriptionConfirmation()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "unsubscribed",
                "id": 42,
                "seq": 7
            }
            """);

        // Act
        var confirm = messages[0].Should().BeOfType<UnsubscriptionConfirmation>().Subject;

        // Assert
        confirm.Id.Should().Be(42);
        confirm.Seq.Should().Be(7);
    }

    #endregion

    #region ErrorMessage

    [Fact]
    public async Task Messages_ReceivesErrorMessage()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "error",
                "msg": {
                    "code": 100,
                    "msg": "Market not found",
                    "market_id": "6F31765E-D070-41B9-A6EA-6AF3274B362B",
                    "market_ticker": "MARKET-ABC"
                }
            }
            """);

        // Act
        var error = messages[0].Should().BeOfType<ErrorMessage>().Subject;

        // Assert
        error.Message.Code.Should().Be(100);
        error.Message.ErrorMessage.Should().Be("Market not found");
        error.Message.MarketId.Should().Be("6F31765E-D070-41B9-A6EA-6AF3274B362B");
        error.Message.MarketTicker.Should().Be("MARKET-ABC");
    }

    #endregion

    #region OKMessage

    [Fact]
    public async Task Messages_ReceivesOkMessage()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "ok",
                "id": 123,
                "seq": 999,
                "market_tickers": ["MARKET-ABC", "MARKET-XYZ"],
                "market_ids": ["6F31765E-D070-41B9-A6EA-6AF3274B362B", "7A42876F-E181-52CA-B7FB-7BG4385C473C"]
            }
            """);

        // Act
        var ok = messages[0].Should().BeOfType<OKMessage>().Subject;

        // Assert
        ok.Id.Should().Be(123);
        ok.Seq.Should().Be(999);
        ok.MarketTickers.Should().HaveCount(2);
        ok.MarketTickers![0].Should().Be("MARKET-ABC");
        ok.MarketTickers![1].Should().Be("MARKET-XYZ");
        ok.MarketIds.Should().HaveCount(2);
    }

    #endregion

    #region UnknownMessage

    [Fact]
    public async Task Messages_UnknownType_PassesThrough()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var messages = await ReceiveOneMessage("""
            {
                "type": "future_feature",
                "data": {"foo": "bar"}
            }
            """);

        // Act
        var unknown = messages[0].Should().BeOfType<UnknownMessage>().Subject;

        // Assert
        unknown.RawType.Should().Be("future_feature");
        unknown.RawPayload.Should().NotBeNull();
    }

    #endregion

    /// <summary>
    /// Mock WebSocket connection for testing.
    /// </summary>
    private sealed class MockWebSocketConnection : IWebSocketConnection
    {
        private readonly Queue<string> _messageQueue = new();
        private readonly List<string> _sentMessages = [];
        private readonly object _lock = new();
        private ConnectionState _state = ConnectionState.Disconnected;
        private bool _connected;

        public ConnectionState State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }

        public WebSocketState WebSocketState => _connected ? WebSocketState.Open : WebSocketState.None;

        public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

        public IReadOnlyList<string> SentMessages => _sentMessages;

        public void SetupConnect()
        {
            // Allows connect to succeed by clearing any previous state.
            // State transitions happen during ConnectAsync.
            _connected = false;
        }

        public void EnqueueMessage(string json)
        {
            lock (_lock)
            {
                _messageQueue.Enqueue(json);
            }
        }

        public Task ConnectAsync(Uri uri, IReadOnlyDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            TransitionState(ConnectionState.Connecting);
            _connected = true;
            TransitionState(ConnectionState.Connected);
            return Task.CompletedTask;
        }

        public Task SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default)
        {
            var json = Encoding.UTF8.GetString(message.Span);
            _sentMessages.Add(json);
            return Task.CompletedTask;
        }

        public ValueTask<WebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_messageQueue.TryDequeue(out var message))
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    bytes.CopyTo(buffer);
                    return ValueTask.FromResult(new WebSocketReceiveResult(
                        bytes.Length,
                        WebSocketMessageType.Text,
                        endOfMessage: true));
                }
            }

            // Simulate waiting for messages
            return new ValueTask<WebSocketReceiveResult>(
                Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken)
                    .ContinueWith(_ => new WebSocketReceiveResult(
                        0,
                        WebSocketMessageType.Close,
                        endOfMessage: true,
                        WebSocketCloseStatus.NormalClosure,
                        "No more messages"),
                        cancellationToken));
        }

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken = default)
        {
            _connected = false;
            TransitionState(ConnectionState.Disconnected);
            return Task.CompletedTask;
        }

        public void MarkAuthenticated()
        {
            TransitionState(ConnectionState.Authenticated);
        }

        public void MarkSubscribed()
        {
            TransitionState(ConnectionState.Subscribed);
        }

        public void Reset()
        {
            _connected = false;
            TransitionState(ConnectionState.Disconnected);
        }

        public ValueTask DisposeAsync()
        {
            _connected = false;
            return ValueTask.CompletedTask;
        }

        private void TransitionState(ConnectionState newState)
        {
            ConnectionState previous;
            lock (_lock)
            {
                previous = _state;
                _state = newState;
            }

            StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                PreviousState = previous,
                NewState = newState
            });
        }
    }
}
