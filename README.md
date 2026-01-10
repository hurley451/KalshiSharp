# KalshiSharp

A production-grade .NET 8 SDK for the [Kalshi](https://kalshi.com) prediction market API.

## Features

- **Full API Coverage**: REST endpoints for exchange, markets, events, orders, portfolio, and users
- **Real-time WebSocket**: Order book, trade, and order update subscriptions with auto-reconnect
- **Async-First**: All operations are async/await with proper cancellation support
- **Thread-Safe**: Safe for concurrent use from multiple threads
- **Strongly Typed**: Complete type coverage with nullable reference types enabled
- **Automatic Signing**: HMAC-SHA256 request signing handled transparently
- **Resilience**: Built-in retry with exponential backoff, rate limiting, and circuit breaker
- **Observability**: OpenTelemetry tracing and metrics integration
- **Dependency Injection**: First-class support for `IServiceCollection`

## Installation

```bash
dotnet add package KalshiSharp.Rest
dotnet add package KalshiSharp.WebSockets  # For real-time updates
```

## Quick Start

### Configuration

```csharp
using KalshiSharp.Core.Configuration;
using KalshiSharp.Core.DependencyInjection;
using KalshiSharp.Core.Http;
using KalshiSharp.Rest;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddLogging();
services.AddKalshiClient(options =>
{
    options.ApiKey = "your-api-key";
    options.ApiSecret = "your-api-secret";
    options.Environment = KalshiEnvironment.Demo; // or Production
});

var provider = services.BuildServiceProvider();
var httpClient = provider.GetRequiredService<IKalshiHttpClient>();
using var client = new KalshiClient(httpClient);
```

Or use the convenience overload:

```csharp
services.AddKalshiClient("your-api-key", "your-api-secret", KalshiEnvironment.Demo);
```

### Get Exchange Status

```csharp
var status = await client.Exchange.GetStatusAsync();
Console.WriteLine($"Trading Active: {status.TradingActive}");

var schedule = await client.Exchange.GetScheduleAsync();
foreach (var entry in schedule.Schedule)
{
    Console.WriteLine($"{entry.StartTime:g} - {entry.EndTime:g}");
}
```

### List Markets with Pagination

```csharp
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Requests;

var query = new MarketQuery
{
    Limit = 10,
    Status = MarketStatus.Open
};

var page1 = await client.Markets.ListMarketsAsync(query);

foreach (var market in page1.Items)
{
    Console.WriteLine($"{market.Ticker}: {market.Title}");
    Console.WriteLine($"  Yes: {market.YesBid}c / {market.YesAsk}c");
}

// Fetch next page
if (page1.HasMore)
{
    var page2 = await client.Markets.ListMarketsAsync(query with { Cursor = page1.Cursor });
}
```

### Get Order Book

```csharp
var orderBook = await client.Markets.GetOrderBookAsync("TICKER-ABC");

Console.WriteLine($"Yes levels: {orderBook.Yes.Count}");
Console.WriteLine($"No levels: {orderBook.No.Count}");

foreach (var level in orderBook.Yes)
{
    Console.WriteLine($"  {level.Price}c: {level.Quantity}");
}
```

### Place and Cancel Orders

```csharp
using KalshiSharp.Models.Requests;

// Create a limit order
var request = new CreateOrderRequest
{
    Ticker = "TICKER-ABC",
    Side = OrderSide.Yes,
    Action = "buy",
    Type = OrderType.Limit,
    Count = 10,
    YesPrice = 45, // 45 cents
    ClientOrderId = Guid.NewGuid().ToString("N")
};

var order = await client.Orders.CreateOrderAsync(request);
Console.WriteLine($"Order ID: {order.OrderId}, Status: {order.Status}");

// Cancel the order
var cancelled = await client.Orders.CancelOrderAsync(order.OrderId);
```

### Portfolio Information

```csharp
// Get balance
var balance = await client.Portfolio.GetBalanceAsync();
Console.WriteLine($"Balance: ${balance.Balance / 100m:F2}");

// List positions
var positions = await client.Portfolio.ListPositionsAsync();
foreach (var position in positions.Items)
{
    Console.WriteLine($"{position.Ticker}: {position.Position} contracts");
}

// List fills
var fills = await client.Portfolio.ListFillsAsync();
```

### WebSocket Real-Time Updates

```csharp
using KalshiSharp.WebSockets;
using KalshiSharp.WebSockets.Subscriptions;
using KalshiSharp.Models.WebSocket;

await using var wsClient = new KalshiWebSocketClient(options, connection, reconnectPolicy, clock, logger);

// Handle state changes
wsClient.StateChanged += (s, e) => Console.WriteLine($"State: {e.NewState}");

// Connect
await wsClient.ConnectAsync();

// Subscribe to order book updates
var orderBookSub = OrderBookSubscription.ForMarkets("TICKER-ABC");
await wsClient.SubscribeAsync(orderBookSub);

// Subscribe to trades
var tradeSub = TradeSubscription.ForMarkets("TICKER-ABC");
await wsClient.SubscribeAsync(tradeSub);

// Process messages
await foreach (var message in wsClient.Messages)
{
    switch (message)
    {
        case OrderBookSnapshotMessage snapshot:
            Console.WriteLine($"Snapshot: {snapshot.MarketTicker}");
            break;
        case OrderBookUpdate update:
            Console.WriteLine($"Delta: {update.Price}c, {update.Delta}");
            break;
        case TradeUpdate trade:
            Console.WriteLine($"Trade: {trade.Count} @ {trade.YesPrice}c");
            break;
    }
}

// Cleanup
await wsClient.UnsubscribeAsync(orderBookSub);
await wsClient.DisconnectAsync();
```

## Error Handling

The SDK provides strongly-typed exceptions for different error scenarios:

```csharp
using KalshiSharp.Core.Errors;

try
{
    var market = await client.Markets.GetMarketAsync("INVALID");
}
catch (KalshiNotFoundException)
{
    Console.WriteLine("Market not found");
}
catch (KalshiAuthException)
{
    Console.WriteLine("Invalid credentials");
}
catch (KalshiRateLimitException ex)
{
    Console.WriteLine($"Rate limited, retry after: {ex.RetryAfter}");
}
catch (KalshiValidationException ex)
{
    foreach (var error in ex.ValidationErrors ?? new())
    {
        Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value)}");
    }
}
catch (KalshiException ex)
{
    Console.WriteLine($"API error: {ex.StatusCode} - {ex.Message}");
}
```

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `ApiKey` | Required | Your Kalshi API key |
| `ApiSecret` | Required | Your Kalshi API secret |
| `Environment` | `Production` | `Production` or `Demo` |
| `BaseUri` | Auto | Override base URI |
| `Timeout` | 30s | HTTP request timeout |
| `ClockSkewTolerance` | 30s | Tolerance for timestamp validation |
| `EnableRateLimiting` | true | Enable client-side rate limiting |

## Project Structure

```
KalshiSharp/
├── src/
│   ├── KalshiSharp.Core/          # Core infrastructure
│   │   ├── Auth/                  # Request signing
│   │   ├── Configuration/         # Client options
│   │   ├── Errors/                # Exception types
│   │   ├── Http/                  # HTTP client pipeline
│   │   ├── RateLimiting/          # Token bucket limiter
│   │   └── Serialization/         # JSON converters
│   ├── KalshiSharp.Models/        # DTOs and enums
│   │   ├── Enums/
│   │   ├── Requests/
│   │   ├── Responses/
│   │   └── WebSocket/
│   ├── KalshiSharp.Rest/          # REST API clients
│   │   ├── Exchange/
│   │   ├── Markets/
│   │   ├── Events/
│   │   ├── Orders/
│   │   ├── Portfolio/
│   │   └── Users/
│   └── KalshiSharp.WebSockets/    # WebSocket client
│       ├── Connections/
│       ├── Subscriptions/
│       └── ReconnectPolicy/
├── tests/
│   └── KalshiSharp.Tests/         # Unit and integration tests
└── examples/
    └── KalshiSharp.Examples/      # Example console app
```

## Running Examples

```bash
# Set credentials
export KALSHI_API_KEY="your-key"
export KALSHI_API_SECRET="your-secret"

# Run specific example
dotnet run --project examples/KalshiSharp.Examples -- exchange
dotnet run --project examples/KalshiSharp.Examples -- markets
dotnet run --project examples/KalshiSharp.Examples -- order
dotnet run --project examples/KalshiSharp.Examples -- websocket

# Run all examples
dotnet run --project examples/KalshiSharp.Examples -- all
```

## Requirements

- .NET 8.0 or later
- Valid Kalshi API credentials

## License

MIT
