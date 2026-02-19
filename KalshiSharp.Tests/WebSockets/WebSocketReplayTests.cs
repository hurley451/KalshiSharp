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
    public async Task SubscribeAsync_SendsSubscribeCommand()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var subscription = new OrderBookSubscription
        {
            Markets = ["MARKET-ABC"]
        };

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

        var subscription = new TradeSubscription
        {
            Markets = ["MARKET-XYZ"]
        };

        await _client.SubscribeAsync(subscription);

        // Act
        await _client.UnsubscribeAsync(subscription);

        // Assert
        var lastMessage = _mockConnection.SentMessages[^1];
        lastMessage.Should().Contain("\"cmd\":\"unsubscribe\"");
        lastMessage.Should().Contain("\"channels\":[\"trade\"]");
    }

    [Fact]
    public async Task Messages_ReceivesOrderBookUpdate()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var updateJson = """
            {
                "type": "orderbook_delta",
                "seq": 12345,
                "msg" : {
                  "ts": 1704067200000,
                  "market_ticker": "MARKET-ABC",
                  "market_id": "6F31765E-D070-41B9-A6EA-6AF3274B362B",
                  "price": 50,
                  "delta": 100,
                  "side": "yes"
                }
            }
            """;

        _mockConnection.EnqueueMessage(updateJson);

        // Act
        var messages = new List<WebSocketMessage>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        try
        {
            await foreach (var msg in _client.Messages.WithCancellation(cts.Token))
            {
                messages.Add(msg);
                if (messages.Count >= 1)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        messages.Should().HaveCount(1);
        var update = messages[0].Should().BeOfType<OrderBookUpdate>().Subject;
        update.Message.MarketTicker.Should().Be("MARKET-ABC");
        update.Message.Price.Should().Be(50);
        update.Message.Delta!.Should().Be(100);
        update.Message.Side!.Should().Be("yes");
        update.Message.IsYesSide!.Should().BeTrue();
        update.Sequence.Should().Be(12345);
    }

    [Fact]
    public async Task Messages_ReceivesOrderBookSnapshot()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var snapshotJson = """
            {                
                "type": "orderbook_snapshot",
                "msg": {
                 "market_id": "6F31765E-D070-41B9-A6EA-6AF3274B362B",
                 "market_ticker": "MARKET-ABC",
                 "yes": [[50, 100], [51, 200]],
                 "no": [[49, 150]]
                }
            }
            """;

        _mockConnection.EnqueueMessage(snapshotJson);

        // Act
        var messages = new List<WebSocketMessage>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        try
        {
            await foreach (var msg in _client.Messages.WithCancellation(cts.Token))
            {
                messages.Add(msg);
                if (messages.Count >= 1)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        var snapshot = messages[0].Should().BeOfType<OrderBookSnapshot>().Subject;
        snapshot.Message.MarketTicker.Should().Be("MARKET-ABC");
        snapshot.Message.Yes!.Should().HaveCount(2);
        snapshot.Message.No!.Should().HaveCount(1);
    }

    [Fact]
    public async Task Messages_ReceivesTradeUpdate()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var tradeJson = """
            {
                "type": "trade",
                "seq": 999,
                "msg": {
                    "ts": 1704067200000,
                    "market_ticker": "MARKET-XYZ",
                    "market_id": "6F31765E-D070-41B9-A6EA-6AF3274B362B",
                    "trade_id": "trade-123",
                    "side": "yes",
                    "count": 50,
                    "yes_price": 65,
                    "no_price": 35,
                    "taker_side": "yes"
                }
            }
            """;

        _mockConnection.EnqueueMessage(tradeJson);

        // Act
        var messages = new List<WebSocketMessage>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        try
        {
            await foreach (var msg in _client.Messages.WithCancellation(cts.Token))
            {
                messages.Add(msg);
                if (messages.Count >= 1)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        var trade = messages[0].Should().BeOfType<TradeUpdate>().Subject;
        trade.Message.MarketTicker!.Should().Be("MARKET-XYZ");
        trade.Message.TradeId!.Should().Be("trade-123");
        trade.Message.Count!.Should().Be(50);
        trade.Message.YesPrice!.Should().Be(65);
    }

    [Fact]
    public async Task Messages_ReceivesSubscriptionConfirmation()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var confirmJson = """
            {
                "type": "subscribed",
                "msg": {
                  "channel": "orderbook_delta"
                }
            }
            """;

        _mockConnection.EnqueueMessage(confirmJson);

        // Act
        var messages = new List<WebSocketMessage>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        try
        {
            await foreach (var msg in _client.Messages.WithCancellation(cts.Token))
            {
                messages.Add(msg);
                if (messages.Count >= 1)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        var confirm = messages[0].Should().BeOfType<SubscriptionConfirmation>().Subject;
        confirm.Message.Channel!.Should().Be("orderbook_delta");
    }

    [Fact]
    public async Task Messages_ReceivesErrorMessage()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var errorJson = """
            {
                "type": "error",                
                "msg": {
                    "code": 100,
                    "msg": "Market not found"
                }
            }
            """;

        _mockConnection.EnqueueMessage(errorJson);

        // Act
        var messages = new List<WebSocketMessage>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        try
        {
            await foreach (var msg in _client.Messages.WithCancellation(cts.Token))
            {
                messages.Add(msg);
                if (messages.Count >= 1)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        var error = messages[0].Should().BeOfType<ErrorMessage>().Subject;
        error.Message.Code.Should().Be(100);
        error.Message.ErrorMessage.Should().Be("Market not found");
    }

    [Fact]
    public async Task Messages_UnknownType_PassesThrough()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var unknownJson = """
            {
                "type": "future_feature",
                "data": {"foo": "bar"}
            }
            """;

        _mockConnection.EnqueueMessage(unknownJson);

        // Act
        var messages = new List<WebSocketMessage>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        try
        {
            await foreach (var msg in _client.Messages.WithCancellation(cts.Token))
            {
                messages.Add(msg);
                if (messages.Count >= 1)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        var unknown = messages[0].Should().BeOfType<UnknownMessage>().Subject;
        unknown.RawType.Should().Be("future_feature");
        unknown.RawPayload.Should().NotBeNull();
    }

    [Fact]
    public async Task Messages_ReceivesOkMessage()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var okJson = """
            {
                "type": "ok",
                "id": 123,
                "seq": 999,
                "market_tickers": ["MARKET-ABC", "MARKET-XYZ"],
                "market_ids": ["6F31765E-D070-41B9-A6EA-6AF3274B362B", "7A42876F-E181-52CA-B7FB-7BG4385C473C"]
            }
            """;

        _mockConnection.EnqueueMessage(okJson);

        // Act
        var messages = new List<WebSocketMessage>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        try
        {
            await foreach (var msg in _client.Messages.WithCancellation(cts.Token))
            {
                messages.Add(msg);
                if (messages.Count >= 1)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        var okMessage = messages[0].Should().BeOfType<OKMessage>().Subject;
        okMessage.Id.Should().Be(123);
        okMessage.Seq.Should().Be(999);
        okMessage.MarketTickers.Should().NotBeNull();
        okMessage.MarketTickers!.Should().HaveCount(2);
        okMessage.MarketTickers![0].Should().Be("MARKET-ABC");
        okMessage.MarketTickers![1].Should().Be("MARKET-XYZ");
        okMessage.MarketIds.Should().NotBeNull();
        okMessage.MarketIds!.Should().HaveCount(2);
    }

    [Fact]
    public async Task Messages_ReceivesTickerUpdate()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var tickerJson = """
            {
                "type": "ticker",
                "seq": 1234,
                "msg": {
                    "market_ticker": "MARKET-ABC",
                    "market_id": "6F31765E-D070-41B9-A6EA-6AF3274B362B",
                    "price": 55,
                    "yes_bid": 54,
                    "yes_ask": 56,
                    "price_dollars": "0.55",
                    "yes_bid_dollars": "0.54",
                    "yes_ask_dollars": "0.56",
                    "volume": 10000,
                    "volume_fp": 10000.00,
                    "open_interest": 5000,
                    "open_interest_fp": 5000.00,
                    "ts": 1771526292,
                    "time": "2026-02-19T18:38:12.398904Z",
                    "dollar_volume": 5500,
                    "dollar_open_interest": 2750
                }
            }
            """;

        _mockConnection.EnqueueMessage(tickerJson);

        // Act
        var messages = new List<WebSocketMessage>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        try
        {
            await foreach (var msg in _client.Messages.WithCancellation(cts.Token))
            {
                messages.Add(msg);
                if (messages.Count >= 1)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        var tickerUpdate = messages[0].Should().BeOfType<TickerUpdate>().Subject;
        tickerUpdate.Message.MarketTicker.Should().Be("MARKET-ABC");
        tickerUpdate.Message.MarketId.Should().Be(Guid.Parse("6F31765E-D070-41B9-A6EA-6AF3274B362B"));
        tickerUpdate.Message.Price.Should().Be(55);
        tickerUpdate.Message.YesBid.Should().Be(54);
        tickerUpdate.Message.YesAsk.Should().Be(56);
        tickerUpdate.Message.Volume.Should().Be(10000);
        tickerUpdate.Message.OpenInterest.Should().Be(5000);
        tickerUpdate.Message.TimeStamp.Should().Be(1771526292);
        tickerUpdate.Message.Time.Should().Be(DateTimeOffset.Parse("2026-02-19T18:38:12.398904Z", CultureInfo.InvariantCulture));
        tickerUpdate.Message.NoBid.Should().Be(44);
        tickerUpdate.Message.NoAsk.Should().Be(46);
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

    [Fact]
    public async Task SubscribeAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var subscription = new OrderBookSubscription { Markets = ["MARKET-ABC"] };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _client.SubscribeAsync(subscription));
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
    public void MessageParsing_OrderUpdate_ParsesCorrectly()
    {
        // Arrange
        var json = """
            {
                "type": "order",
                "seq": 100,
                "ts": 1704067200000,
                "order_id": "order-456",
                "market_ticker": "MARKET-ABC",
                "side": "yes",
                "order_type": "limit",
                "action": "place",
                "status": "resting",
                "count": 50,
                "remaining_count": 25,
                "yes_price": 55,
                "no_price": 45
            }
            """;

        // Act
        var message = JsonSerializer.Deserialize<WebSocketMessage>(json, KalshiJsonOptions.Default);

        // Assert
        var orderUpdate = message.Should().BeOfType<OrderUpdate>().Subject;
        orderUpdate.OrderId.Should().Be("order-456");
        orderUpdate.MarketTicker.Should().Be("MARKET-ABC");
        orderUpdate.Side.Should().Be(OrderSide.Yes);
        orderUpdate.Action.Should().Be("place");
        orderUpdate.Status.Should().Be(OrderStatus.Resting);
        orderUpdate.RemainingCount.Should().Be(25);
        orderUpdate.YesPrice.Should().Be(55);
        orderUpdate.FilledCount.Should().Be(25);
    }

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
