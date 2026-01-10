// KalshiSharp SDK Examples
// Demonstrates common usage patterns for the Kalshi API client.

using KalshiSharp.Core.Auth;
using KalshiSharp.Core.Configuration;
using KalshiSharp.Core.DependencyInjection;
using KalshiSharp.Core.Errors;
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.WebSocket;
using KalshiSharp.Rest;
using KalshiSharp.WebSockets;
using KalshiSharp.WebSockets.Connections;
using KalshiSharp.WebSockets.ReconnectPolicy;
using KalshiSharp.WebSockets.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

Console.WriteLine("KalshiSharp SDK Examples");
Console.WriteLine("========================");
Console.WriteLine();

// Configure your API credentials via environment variables or replace with your own
var apiKey = Environment.GetEnvironmentVariable("KALSHI_API_KEY") ?? "your-api-key";
var apiSecret = Environment.GetEnvironmentVariable("KALSHI_API_SECRET") ?? "your-api-secret";

// Run examples based on command-line arguments or run all
var exampleArgs = Environment.GetCommandLineArgs().Skip(1).ToList();
if (exampleArgs.Count == 0)
{
    Console.WriteLine("Available examples:");
    Console.WriteLine("  exchange    - Get exchange status");
    Console.WriteLine("  markets     - List markets with pagination");
    Console.WriteLine("  order       - Place and cancel an order");
    Console.WriteLine("  websocket   - Subscribe to WebSocket updates");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run -- <example>");
    Console.WriteLine("       dotnet run -- all");
    return;
}

var runAll = exampleArgs.Contains("all", StringComparer.OrdinalIgnoreCase);

if (runAll || exampleArgs.Contains("exchange", StringComparer.OrdinalIgnoreCase))
{
    await ExchangeStatusExample(apiKey, apiSecret);
}

if (runAll || exampleArgs.Contains("markets", StringComparer.OrdinalIgnoreCase))
{
    await ListMarketsExample(apiKey, apiSecret);
}

if (runAll || exampleArgs.Contains("order", StringComparer.OrdinalIgnoreCase))
{
    await OrderExample(apiKey, apiSecret);
}

if (runAll || exampleArgs.Contains("websocket", StringComparer.OrdinalIgnoreCase))
{
    await WebSocketExample(apiKey, apiSecret);
}

// ============================================================================
// Example 1: Get Exchange Status
// ============================================================================
static async Task ExchangeStatusExample(string apiKey, string apiSecret)
{
    Console.WriteLine("=== Example 1: Get Exchange Status ===");
    Console.WriteLine();

    // Create client using DI container
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddKalshiClient(options =>
    {
        options.ApiKey = apiKey;
        options.ApiSecret = apiSecret;
        options.Environment = KalshiEnvironment.Demo; // Use Demo for testing
    });

    await using var provider = services.BuildServiceProvider();
    var httpClient = provider.GetRequiredService<KalshiSharp.Core.Http.IKalshiHttpClient>();
    using var client = new KalshiClient(httpClient);

    try
    {
        // Get exchange status
        var status = await client.Exchange.GetStatusAsync();
        Console.WriteLine($"Trading Active: {status.TradingActive}");

        // Get exchange schedule
        var schedule = await client.Exchange.GetScheduleAsync();
        Console.WriteLine($"Schedule entries: {schedule.Schedule.Count}");
        foreach (var entry in schedule.Schedule.Take(3))
        {
            Console.WriteLine($"  {entry.StartTime:g} - {entry.EndTime:g}");
        }
    }
    catch (KalshiException ex)
    {
        Console.WriteLine($"API Error: {ex.Message} (Status: {ex.StatusCode})");
    }

    Console.WriteLine();
}

// ============================================================================
// Example 2: List Markets with Pagination
// ============================================================================
static async Task ListMarketsExample(string apiKey, string apiSecret)
{
    Console.WriteLine("=== Example 2: List Markets with Pagination ===");
    Console.WriteLine();

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddKalshiClient(apiKey, apiSecret, KalshiEnvironment.Demo);

    await using var provider = services.BuildServiceProvider();
    var httpClient = provider.GetRequiredService<KalshiSharp.Core.Http.IKalshiHttpClient>();
    using var client = new KalshiClient(httpClient);

    try
    {
        // Fetch first page of markets
        var query = new MarketQuery
        {
            Limit = 5,
            Status = MarketStatus.Open
        };

        var page1 = await client.Markets.ListMarketsAsync(query);
        Console.WriteLine($"Page 1: {page1.Items.Count} markets");

        foreach (var market in page1.Items)
        {
            Console.WriteLine($"  - {market.Ticker}: {market.Title}");
            Console.WriteLine($"    Yes: {market.YesBid}c / {market.YesAsk}c, Volume: {market.Volume}");
        }

        // Fetch next page if available
        if (page1.HasMore)
        {
            var page2 = await client.Markets.ListMarketsAsync(query with { Cursor = page1.Cursor });
            Console.WriteLine($"Page 2: {page2.Items.Count} markets");

            foreach (var market in page2.Items)
            {
                Console.WriteLine($"  - {market.Ticker}: {market.Title}");
            }
        }

        // Get a specific market's order book
        if (page1.Items.Count > 0)
        {
            var ticker = page1.Items[0].Ticker;
            var orderBook = await client.Markets.GetOrderBookAsync(ticker);
            Console.WriteLine($"\nOrder Book for {ticker}:");
            Console.WriteLine($"  Yes levels: {orderBook.Yes.Count}");
            Console.WriteLine($"  No levels: {orderBook.No.Count}");
        }
    }
    catch (KalshiNotFoundException)
    {
        Console.WriteLine("Market not found");
    }
    catch (KalshiException ex)
    {
        Console.WriteLine($"API Error: {ex.Message}");
    }

    Console.WriteLine();
}

// ============================================================================
// Example 3: Place and Cancel an Order
// ============================================================================
static async Task OrderExample(string apiKey, string apiSecret)
{
    Console.WriteLine("=== Example 3: Place and Cancel an Order ===");
    Console.WriteLine();

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddKalshiClient(apiKey, apiSecret, KalshiEnvironment.Demo);

    await using var provider = services.BuildServiceProvider();
    var httpClient = provider.GetRequiredService<KalshiSharp.Core.Http.IKalshiHttpClient>();
    using var client = new KalshiClient(httpClient);

    try
    {
        // First, list available markets to get a valid ticker
        var markets = await client.Markets.ListMarketsAsync(new MarketQuery { Limit = 1, Status = MarketStatus.Open });

        if (markets.Items.Count == 0)
        {
            Console.WriteLine("No open markets available for demo");
            return;
        }

        var ticker = markets.Items[0].Ticker;
        Console.WriteLine($"Using market: {ticker}");

        // Create a limit order
        var createRequest = new CreateOrderRequest
        {
            Ticker = ticker,
            Side = OrderSide.Yes,
            Action = "buy",
            Type = OrderType.Limit,
            Count = 1,
            YesPrice = 50, // 50 cents
            ClientOrderId = $"example-{Guid.NewGuid():N}"
        };

        Console.WriteLine($"Placing order: Buy 1 Yes @ $0.{createRequest.YesPrice:D2}");
        var order = await client.Orders.CreateOrderAsync(createRequest);
        Console.WriteLine($"Order created: {order.OrderId}");
        Console.WriteLine($"  Status: {order.Status}");
        Console.WriteLine($"  Filled: {order.FilledCount}/{order.Count}");

        // List orders to verify
        var orders = await client.Orders.ListOrdersAsync();
        Console.WriteLine($"\nYou have {orders.Items.Count} order(s)");

        // Cancel the order
        Console.WriteLine($"\nCancelling order {order.OrderId}...");
        var cancelled = await client.Orders.CancelOrderAsync(order.OrderId);
        Console.WriteLine($"Order cancelled. Status: {cancelled.Status}");

        // Check portfolio balance
        var balance = await client.Portfolio.GetBalanceAsync();
        Console.WriteLine($"\nPortfolio Balance: ${balance.Balance / 100m:F2}");
    }
    catch (KalshiValidationException ex)
    {
        Console.WriteLine($"Validation Error: {ex.Message}");
        if (ex.ValidationErrors != null)
        {
            foreach (var error in ex.ValidationErrors)
            {
                Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value)}");
            }
        }
    }
    catch (KalshiAuthException)
    {
        Console.WriteLine("Authentication failed. Check your API credentials.");
    }
    catch (KalshiException ex)
    {
        Console.WriteLine($"API Error: {ex.Message} (Status: {ex.StatusCode})");
    }

    Console.WriteLine();
}

// ============================================================================
// Example 4: WebSocket Subscriptions
// ============================================================================
static async Task WebSocketExample(string apiKey, string apiSecret)
{
    Console.WriteLine("=== Example 4: WebSocket Subscriptions ===");
    Console.WriteLine();

    // Create dependencies for WebSocket client
    var clientOptions = new KalshiClientOptions
    {
        ApiKey = apiKey,
        ApiSecret = apiSecret,
        Environment = KalshiEnvironment.Demo
    };
    var optionsWrapper = Options.Create(clientOptions);
    var clock = new SystemClock();
    var connectionLogger = NullLogger<WebSocketConnection>.Instance;
    var connection = new WebSocketConnection(connectionLogger);
    var reconnectPolicy = new ExponentialBackoffPolicy();
    var logger = NullLogger<KalshiWebSocketClient>.Instance;

    // Create a WebSocket client
    await using var wsClient = new KalshiWebSocketClient(
        optionsWrapper,
        connection,
        reconnectPolicy,
        clock,
        logger);

    // Handle state changes
    wsClient.StateChanged += (sender, e) =>
    {
        Console.WriteLine($"WebSocket state: {e.PreviousState} -> {e.NewState}");
    };

    try
    {
        // Connect to WebSocket server
        Console.WriteLine("Connecting to WebSocket...");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await wsClient.ConnectAsync(cts.Token);
        Console.WriteLine($"Connected! State: {wsClient.State}");

        // Subscribe to order book updates for a specific market
        // First get a market ticker from REST API
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKalshiClient(apiKey, apiSecret, KalshiEnvironment.Demo);
        await using var provider = services.BuildServiceProvider();
        var httpClient = provider.GetRequiredService<KalshiSharp.Core.Http.IKalshiHttpClient>();
        using var restClient = new KalshiClient(httpClient);

        var markets = await restClient.Markets.ListMarketsAsync(new MarketQuery { Limit = 1, Status = MarketStatus.Open });
        if (markets.Items.Count == 0)
        {
            Console.WriteLine("No open markets available");
            return;
        }

        var ticker = markets.Items[0].Ticker;
        Console.WriteLine($"Subscribing to order book updates for: {ticker}");

        // Subscribe to order book delta updates
        var orderBookSub = OrderBookSubscription.ForMarkets(ticker);
        await wsClient.SubscribeAsync(orderBookSub, cts.Token);

        // Subscribe to trade updates
        var tradeSub = TradeSubscription.ForMarkets(ticker);
        await wsClient.SubscribeAsync(tradeSub, cts.Token);

        Console.WriteLine("Subscribed! Listening for updates (10 seconds)...");
        Console.WriteLine();

        // Process incoming messages
        using var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await foreach (var message in wsClient.Messages.WithCancellation(readCts.Token))
            {
                switch (message)
                {
                    case OrderBookSnapshotMessage snapshot:
                        Console.WriteLine($"[OrderBook Snapshot] {snapshot.MarketTicker}");
                        Console.WriteLine($"  Yes levels: {snapshot.Yes.Count}");
                        Console.WriteLine($"  No levels: {snapshot.No.Count}");
                        break;

                    case OrderBookUpdate update:
                        Console.WriteLine($"[OrderBook Delta] {update.MarketTicker}");
                        Console.WriteLine($"  Price: {update.Price}, Delta: {update.Delta}, Side: {update.Side}");
                        break;

                    case TradeUpdate trade:
                        Console.WriteLine($"[Trade] {trade.MarketTicker}");
                        Console.WriteLine($"  Count: {trade.Count}, Yes Price: {trade.YesPrice}c");
                        break;

                    case HeartbeatMessage:
                        Console.WriteLine("[Heartbeat]");
                        break;

                    case UnknownMessage unknown:
                        Console.WriteLine($"[Unknown] Type: {unknown.RawType}");
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nListening timeout reached.");
        }

        // Unsubscribe and disconnect
        Console.WriteLine("\nUnsubscribing...");
        await wsClient.UnsubscribeAsync(orderBookSub, CancellationToken.None);
        await wsClient.UnsubscribeAsync(tradeSub, CancellationToken.None);

        Console.WriteLine("Disconnecting...");
        await wsClient.DisconnectAsync(CancellationToken.None);
        Console.WriteLine("Done!");
    }
    catch (KalshiAuthException)
    {
        Console.WriteLine("WebSocket authentication failed. Check your API credentials.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WebSocket Error: {ex.Message}");
    }

    Console.WriteLine();
}
