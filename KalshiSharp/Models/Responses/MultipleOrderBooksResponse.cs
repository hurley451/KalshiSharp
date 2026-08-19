using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>Response containing order books for multiple markets.</summary>
public sealed record MultipleOrderBooksResponse
{
    /// <summary>Order books returned by the API.</summary>
    [JsonPropertyName("orderbooks")]
    public IReadOnlyList<MarketOrderBookResponse> OrderBooks { get; init; } = [];
}

/// <summary>An order book associated with its market ticker.</summary>
public sealed record MarketOrderBookResponse
{
    /// <summary>Market ticker.</summary>
    [JsonPropertyName("ticker")]
    public required string Ticker { get; init; }

    /// <summary>Fixed-point order-book levels.</summary>
    [JsonPropertyName("orderbook_fp")]
    public required FixedPointOrderBookData OrderBookFp { get; init; }
}
