using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using KalshiSharp.Core.Auth;
using KalshiSharp.Core.Configuration;
using KalshiSharp.Core.Serialization;
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

namespace KalshiSharp.Tests.WebSockets;

/// <summary>
/// WebSocket message replay tests to verify message parsing and dispatch.
/// </summary>
public sealed class WebSocketReplayTests : IAsyncDisposable
{
    private readonly MockWebSocketConnection _mockConnection;
    private readonly KalshiWebSocketClient _client;

    public WebSocketReplayTests()
    {
        var options = Options.Create(new KalshiClientOptions
        {
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret",
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
        sentMessages.Should().HaveCountGreaterOrEqualTo(2); // Auth + Subscribe

        var subscribeMessage = sentMessages[^1];
        subscribeMessage.Should().Contain("\"cmd\":\"subscribe\"");
        subscribeMessage.Should().Contain("\"channel\":\"orderbook_delta\"");
        subscribeMessage.Should().Contain("MARKET-ABC");
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
        lastMessage.Should().Contain("\"channel\":\"trade\"");
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
                "ts": 1704067200000,
                "market_ticker": "MARKET-ABC",
                "price": 50,
                "delta": 100,
                "side": "yes"
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
        update.MarketTicker.Should().Be("MARKET-ABC");
        update.Price.Should().Be(50);
        update.Delta.Should().Be(100);
        update.Side.Should().Be("yes");
        update.IsYesSide.Should().BeTrue();
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
                "market_ticker": "MARKET-ABC",
                "yes": [[50, 100], [51, 200]],
                "no": [[49, 150]]
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
        var snapshot = messages[0].Should().BeOfType<OrderBookSnapshotMessage>().Subject;
        snapshot.MarketTicker.Should().Be("MARKET-ABC");
        snapshot.Yes.Should().HaveCount(2);
        snapshot.No.Should().HaveCount(1);
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
                "ts": 1704067200000,
                "market_ticker": "MARKET-XYZ",
                "trade_id": "trade-123",
                "side": "yes",
                "count": 50,
                "yes_price": 65,
                "no_price": 35,
                "taker_side": "yes"
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
        trade.MarketTicker.Should().Be("MARKET-XYZ");
        trade.TradeId.Should().Be("trade-123");
        trade.Count.Should().Be(50);
        trade.YesPrice.Should().Be(65);
    }

    [Fact]
    public async Task Messages_ReceivesHeartbeat()
    {
        // Arrange
        _mockConnection.SetupConnect();
        await _client.ConnectAsync();

        var heartbeatJson = """
            {
                "type": "heartbeat",
                "ts": 1704067200000
            }
            """;

        _mockConnection.EnqueueMessage(heartbeatJson);

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
        messages[0].Should().BeOfType<HeartbeatMessage>();
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
                "channel": "orderbook_delta",
                "markets": ["MARKET-ABC", "MARKET-XYZ"]
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
        confirm.Channel.Should().Be("orderbook_delta");
        confirm.Markets.Should().Contain("MARKET-ABC");
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
                "code": "invalid_subscription",
                "msg": "Market not found"
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
        error.Code.Should().Be("invalid_subscription");
        error.Message.Should().Be("Market not found");
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

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
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
