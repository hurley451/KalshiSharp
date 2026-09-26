using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>Total value of resting orders across exchange indexes.</summary>
public sealed record TotalRestingOrderValueResponse
{
    /// <summary>Total resting order value in cents.</summary>
    [JsonPropertyName("total_resting_order_value")]
    public required long TotalRestingOrderValue { get; init; }

    /// <summary>Per-exchange-index resting order value breakdowns.</summary>
    [JsonPropertyName("resting_order_value_breakdown")]
    public IReadOnlyList<RestingOrderValueBreakdown> RestingOrderValueBreakdown { get; init; } = [];
}

/// <summary>Resting order value for one exchange index.</summary>
public sealed record RestingOrderValueBreakdown
{
    /// <summary>Exchange shard index.</summary>
    [JsonPropertyName("exchange_index")]
    public required int ExchangeIndex { get; init; }

    /// <summary>Fixed-point dollar balance for resting orders on the exchange index.</summary>
    [JsonPropertyName("balance")]
    public required string Balance { get; init; }
}
