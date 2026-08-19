# KalshiSharp

A production-grade .NET 8 SDK for the [Kalshi](https://kalshi.com) prediction market API.

## Features

- **Core API Coverage**: REST clients for exchange, markets, events, orders, portfolio, users, and historical data
- **Real-time WebSocket**: Order book, ticker, trade, fill, position, and user-order subscriptions with auto-reconnect
- **Async-First**: All operations are async/await with proper cancellation support
- **Thread-Safe**: Safe for concurrent use from multiple threads
- **Strongly Typed**: Complete type coverage with nullable reference types enabled
- **Automatic Signing**: RSA-PSS request signing handled transparently
- **Resilience**: Built-in retry with exponential backoff, rate limiting, and circuit breaker
- **Observability**: OpenTelemetry tracing and metrics integration
- **Dependency Injection**: First-class support for `IServiceCollection`

## Installation

```bash
dotnet add package KalshiSharp
```

## Quick Start

### Basic Usage

```csharp
using KalshiSharp.Configuration;
using KalshiSharp.Rest;

// Create a client with your API credentials
using var client = new KalshiClient(new KalshiClientOptions
{
    ApiKey = "your-api-key",
    ApiSecret = "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----", // RSA private key in PEM format
    Environment = KalshiEnvironment.Demo // or Production
});

var status = await client.Exchange.GetStatusAsync();
Console.WriteLine($"Trading Active: {status.TradingActive}");
```

### With Dependency Injection (ASP.NET Core)

For applications using dependency injection:

```csharp
using KalshiSharp.Configuration;
using KalshiSharp.DependencyInjection;

// In Program.cs or Startup.cs
services.AddKalshiClient(options =>
{
    options.ApiKey = configuration["Kalshi:ApiKey"]!;
    options.ApiSecret = configuration["Kalshi:ApiSecret"]!;
    options.Environment = KalshiEnvironment.Production;
});

// Inject IKalshiClient directly
public class MyService(IKalshiClient client)
{
    public async Task DoSomethingAsync()
    {
        var markets = await client.Markets.ListMarketsAsync();
    }
}
```

### Get Exchange Schedule

```csharp
var schedule = await client.Exchange.GetScheduleAsync();
Console.WriteLine($"Standard hours entries: {schedule.Schedule.StandardHours.Count}");
Console.WriteLine($"Maintenance windows: {schedule.Schedule.MaintenanceWindows.Count}");
```

### List Markets with Pagination

```csharp
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Requests;

var query = new MarketQuery
{
    Limit = 10,
    Status = MarketStatus.Active
};

var page1 = await client.Markets.ListMarketsAsync(query);

foreach (var market in page1.Items)
{
    Console.WriteLine($"{market.Ticker}: {market.Title}");
    Console.WriteLine($"  Yes: ${market.YesBidDollars} / ${market.YesAskDollars}");
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

Console.WriteLine($"Yes levels: {orderBook.OrderbookFp.YesDollars.Count}");
Console.WriteLine($"No levels: {orderBook.OrderbookFp.NoDollars.Count}");

// Each level is [fixed-point dollar price, fixed-point quantity]
foreach (var level in orderBook.OrderbookFp.YesDollars)
{
    Console.WriteLine($"  ${level[0]}: {level[1]} contracts");
}
```

### Place and Cancel Orders

```csharp
using KalshiSharp.Models.Requests;

// Create an order through the current V2 event-order API
var request = new CreateOrderRequestV2
{
    Ticker = "TICKER-ABC",
    Side = OrderBookSide.Bid,
    Count = "10.00",
    Price = "0.4500",
    TimeInForce = EventOrderTimeInForce.GoodTillCanceled,
    SelfTradePreventionType = SelfTradePreventionType.TakerAtCross,
    ClientOrderId = Guid.NewGuid().ToString("N"),
    ExchangeIndex = -1 // auto-route by ticker
};

var ordersV2 = client.OrdersV2
    ?? throw new NotSupportedException("This client does not provide the V2 order capability.");

var order = await ordersV2.CreateOrderAsync(request);
Console.WriteLine($"Order ID: {order.OrderId}, remaining: {order.RemainingCount}");

// Cancel the order
var cancelled = await ordersV2.CancelOrderAsync(order.OrderId, new CancelOrderQueryV2
{
    ExchangeIndex = -1,
    MarketTicker = request.Ticker
});
```

### Portfolio Information

```csharp
// Get balance
var balance = await client.Portfolio.GetBalanceAsync();
Console.WriteLine($"Balance: ${balance.BalanceDollars}");

// List positions
var positions = await client.Portfolio.ListPositionsAsync();
foreach (var position in positions.Items)
{
    Console.WriteLine($"{position.Ticker}: {position.PositionFp} contracts");
}

// List fills
var fills = await client.Portfolio.ListFillsAsync();
```

### Historical Data

Historical access is explicit. Existing live methods are never rerouted automatically.

```csharp
var historical = client.Historical
    ?? throw new NotSupportedException("This client does not provide historical data access.");

var cutoff = await historical.GetCutoffAsync();
Console.WriteLine($"Markets before {cutoff.MarketSettledTs:u} are historical");

var archived = await historical.ListMarketsAsync(new HistoricalMarketQuery
{
    SeriesTicker = "KXHIGHNY",
    Limit = 100
});
```

### WebSocket Real-Time Updates

```csharp
using KalshiSharp.Configuration;
using KalshiSharp.WebSockets;
using KalshiSharp.WebSockets.Subscriptions;
using KalshiSharp.Models.WebSocket;

// Create WebSocket client
await using var wsClient = new KalshiWebSocketClient(new KalshiClientOptions
{
    ApiKey = "your-api-key",
    ApiSecret = "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----",
    Environment = KalshiEnvironment.Production
});

// Connect and subscribe
await wsClient.ConnectAsync();
await wsClient.SubscribeAsync(OrderBookSubscription.ForMarkets("TICKER-ABC"));
await wsClient.SubscribeAsync(TradeSubscription.ForMarkets("TICKER-ABC"));

// Process messages
await foreach (var message in wsClient.Messages)
{
    switch (message)
    {
        case OrderBookSnapshot snapshot:
            Console.WriteLine($"Snapshot: {snapshot.Message.MarketTicker}");
            break;
        case OrderBookUpdate update:
            Console.WriteLine($"Delta: ${update.Message.PriceDollars}, {update.Message.DeltaFp}");
            break;
        case TradeUpdate trade:
            Console.WriteLine($"Trade: {trade.Message.CountFp} @ ${trade.Message.YesPriceDollars}");
            break;
    }
}
```

## Error Handling

The SDK provides strongly-typed exceptions for different error scenarios:

```csharp
using KalshiSharp.Errors;

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
| `ApiKey` | Required | Your Kalshi API key ID |
| `ApiSecret` | Required | Your RSA private key in PEM format |
| `Environment` | `Production` | `Production` or `Demo` |
| `BaseUri` | Auto | Override base URI |
| `Timeout` | 30s | HTTP request timeout |
| `ClockSkewTolerance` | 30s | Tolerance for timestamp validation |
| `EnableRateLimiting` | true | Enable client-side rate limiting |

## Project Structure

```
KalshiSharp/
├── KalshiSharp/                   # Main SDK library
│   ├── Auth/                      # RSA-PSS request signing
│   ├── Configuration/             # Client options
│   ├── DependencyInjection/       # IServiceCollection extensions
│   ├── Errors/                    # Exception types
│   ├── Http/                      # HTTP client pipeline
│   ├── Models/                    # DTOs and enums
│   │   ├── Enums/
│   │   ├── Requests/
│   │   ├── Responses/
│   │   └── WebSocket/
│   ├── RateLimiting/              # Token bucket limiter
│   ├── Rest/                      # REST API clients
│   │   ├── Exchange/
│   │   ├── Historical/
│   │   ├── Markets/
│   │   ├── Events/
│   │   ├── Orders/
│   │   ├── Portfolio/
│   │   └── Users/
│   ├── Serialization/             # JSON converters
│   └── WebSockets/                # WebSocket client
│       ├── Connections/
│       ├── ReconnectPolicy/
│       └── Subscriptions/
├── KalshiSharp.Tests/             # Unit and integration tests
└── KalshiSharp.Examples/          # Example console app
```

## Running Examples

```bash
# Set credentials via user-secrets (recommended)
cd KalshiSharp.Examples
dotnet user-secrets set "Kalshi:ApiKey" "your-api-key-id"
dotnet user-secrets set "Kalshi:ApiSecret" "-----BEGIN PRIVATE KEY-----
...your PEM key...
-----END PRIVATE KEY-----"

# Or via environment variables (double underscore for nesting)
export KALSHI__APIKEY="your-api-key-id"
export KALSHI__APISECRET="-----BEGIN PRIVATE KEY-----..."

# Run specific example
dotnet run --project KalshiSharp.Examples -- exchange
dotnet run --project KalshiSharp.Examples -- markets
dotnet run --project KalshiSharp.Examples -- order
dotnet run --project KalshiSharp.Examples -- websocket

# Run all examples
dotnet run --project KalshiSharp.Examples -- all
```

## Requirements

- .NET 8.0 or later
- Valid Kalshi API credentials (obtain from [Kalshi API Settings](https://kalshi.com/account/api))

## License

MIT
