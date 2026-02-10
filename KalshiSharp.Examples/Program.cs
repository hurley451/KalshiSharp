// KalshiSharp SDK Examples
// Demonstrates common usage patterns for the Kalshi API client.

using System.Diagnostics.Tracing;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Errors;
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.WebSocket;
using KalshiSharp.Rest;
using KalshiSharp.WebSockets;
using KalshiSharp.WebSockets.Connections;
using KalshiSharp.WebSockets.ReconnectPolicy;
using KalshiSharp.WebSockets.Subscriptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

Console.WriteLine("KalshiSharp SDK Examples");
Console.WriteLine("========================");
Console.WriteLine();

// Build configuration from user-secrets and environment variables
// To set user-secrets: dotnet user-secrets set "Kalshi:ApiKey" "your-api-key"
//                      dotnet user-secrets set "Kalshi:ApiSecret" "your-api-secret"
// Environment variables: KALSHI__APIKEY and KALSHI__APISECRET (double underscore for nesting)
var configuration = new ConfigurationBuilder()
    .AddUserSecrets(typeof(Program).Assembly)
    .AddEnvironmentVariables()
    .Build();

var apiKey = configuration["Kalshi:ApiKey"] ?? throw new InvalidOperationException(
    "API key not configured. Set via user-secrets (dotnet user-secrets set \"Kalshi:ApiKey\" \"your-key\") " +
    "or environment variable KALSHI__APIKEY");
var apiSecret = configuration["Kalshi:ApiSecret"] ?? throw new InvalidOperationException(
    "API secret not configured. Set via user-secrets (dotnet user-secrets set \"Kalshi:ApiSecret\" \"your-secret\") " +
    "or environment variable KALSHI__APISECRET");

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

// Direct API test to isolate signing issues
if (exampleArgs.Contains("rawtest", StringComparer.OrdinalIgnoreCase))
{
    await RawApiTest(apiKey, apiSecret);
    return;
}

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

    // Create client directly - the simplest way to use KalshiSharp
    using var client = new KalshiClient(new KalshiClientOptions
    {
        ApiKey = apiKey,
        ApiSecret = apiSecret,
        Environment = KalshiEnvironment.Demo // Use Demo for testing
    });

    try
    {
        // Get exchange status
        var status = await client.Exchange.GetStatusAsync();
        Console.WriteLine($"Trading Active: {status.TradingActive}");

        // Get exchange schedule
        var schedule = await client.Exchange.GetScheduleAsync();
        Console.WriteLine($"Standard hours entries: {schedule.Schedule.StandardHours.Count}");
        Console.WriteLine($"Maintenance windows: {schedule.Schedule.MaintenanceWindows.Count}");
        foreach (var entry in schedule.Schedule.StandardHours.Take(1))
        {
            Console.WriteLine($"  Effective: {entry.StartTime:g} - {entry.EndTime:g}");
            if (entry.Monday.Count > 0)
                Console.WriteLine($"    Monday: {entry.Monday[0].OpenTime} - {entry.Monday[0].CloseTime}");
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

    using var client = new KalshiClient(new KalshiClientOptions
    {
        ApiKey = apiKey,
        ApiSecret = apiSecret,
        Environment = KalshiEnvironment.Production
    });

    try
    {
        // Fetch first page of markets
        var query = new MarketQuery
        {
            Limit = 5,
            Status = MarketStatus.Active,
            MveFilter = "exclude" // Filter out multivariate/parlay markets
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
            Console.WriteLine($"  Yes levels: {orderBook.Orderbook.Yes.Count}");
            Console.WriteLine($"  No levels: {orderBook.Orderbook.No.Count}");
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

    using var client = new KalshiClient(new KalshiClientOptions
    {
        ApiKey = apiKey,
        ApiSecret = apiSecret,
        Environment = KalshiEnvironment.Demo
    });

    try
    {
        // First, list available markets to get a valid ticker
        var markets = await client.Markets.ListMarketsAsync(new MarketQuery { Limit = 1, Status = MarketStatus.Active });

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
        Console.WriteLine($"  Filled: {order.FillCount}/{order.InitialCount}");

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
        Environment = KalshiEnvironment.Production
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
        using var restClient = new KalshiClient(new KalshiClientOptions
        {
            ApiKey = apiKey,
            ApiSecret = apiSecret,
            Environment = KalshiEnvironment.Production
        });

        var markets = await restClient.Markets.ListMarketsAsync(new MarketQuery { Limit = 1, Status = MarketStatus.Active });
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
                    case OrderBookSnapshot snapshot:
                        Console.WriteLine($"[OrderBook Snapshot] {snapshot.Message.MarketTicker}");
                        Console.WriteLine($"  Yes levels: {snapshot.Message.Yes?.Count}");
                        Console.WriteLine($"  No levels: {snapshot.Message.No?.Count}");
                        break;

                    case OrderBookUpdate update:
                        Console.WriteLine($"[OrderBook Delta] {update.Message.MarketTicker}");
                        Console.WriteLine($"  Price: {update.Message.Price}, Delta: {update.Message.Delta}, Side: {update.Message.Side}");
                        break;

                    case TradeUpdate trade:
                        Console.WriteLine($"[Trade] {trade.Message.MarketTicker}");
                        Console.WriteLine($"  Count: {trade.Message.Count}, Yes Price: {trade.Message.YesPrice}c");
                        break;                 

                    case UnknownMessage unknown:
                        Console.WriteLine($"[Unknown] Type: {unknown.RawType}");
                        if (unknown.RawPayload.HasValue)
                        {
                            Console.WriteLine($"  Payload: {unknown.RawPayload.Value}");
                        }
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

// ============================================================================
// Raw API Test - Direct HTTP call to isolate signing issues
// ============================================================================
static async Task RawApiTest(string apiKeyId, string privateKeyPem)
{
    Console.WriteLine("=== Raw API Test (Direct HTTP) ===");
    Console.WriteLine();

    const string baseUrl = "https://api.elections.kalshi.com";
    const string path = "/trade-api/v2/exchange/status";
    const string method = "GET";

    // Load RSA key
    using var rsa = RSA.Create();
    rsa.ImportFromPem(privateKeyPem.AsSpan());
    Console.WriteLine($"RSA Key loaded: {rsa.KeySize} bits");

    // Build timestamp
    var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var timestampStr = timestampMs.ToString(CultureInfo.InvariantCulture);

    // Build message to sign: timestamp + method + path
    var message = timestampStr + method + path;
    Console.WriteLine($"Message to sign: '{message}'");

    // Sign with RSA-PSS
    var messageBytes = Encoding.UTF8.GetBytes(message);
    var signatureBytes = rsa.SignData(messageBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    var signature = Convert.ToBase64String(signatureBytes);
    Console.WriteLine($"Signature: {signature[..30]}...");

    // Make HTTP request
    using var httpClient = new HttpClient();
    using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl + path);

    request.Headers.TryAddWithoutValidation("KALSHI-ACCESS-KEY", apiKeyId);
    request.Headers.TryAddWithoutValidation("KALSHI-ACCESS-TIMESTAMP", timestampStr);
    request.Headers.TryAddWithoutValidation("KALSHI-ACCESS-SIGNATURE", signature);
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    Console.WriteLine();
    Console.WriteLine("Request headers:");
    Console.WriteLine($"  KALSHI-ACCESS-KEY: {apiKeyId}");
    Console.WriteLine($"  KALSHI-ACCESS-TIMESTAMP: {timestampStr}");
    Console.WriteLine($"  KALSHI-ACCESS-SIGNATURE: {signature[..30]}...");
    Console.WriteLine();

    var response = await httpClient.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();

    Console.WriteLine($"Response: {(int)response.StatusCode} {response.StatusCode}");
    Console.WriteLine($"Body: {content}");
}
