using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>Queue position for an order.</summary>
public sealed record QueuePositionResponse
{
    /// <summary>Order identifier when returned by a list operation.</summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; init; }

    /// <summary>Market ticker when returned by a list operation.</summary>
    [JsonPropertyName("market_ticker")]
    public string? MarketTicker { get; init; }

    /// <summary>Fixed-point queue position.</summary>
    [JsonPropertyName("queue_position_fp")]
    public required string QueuePositionFp { get; init; }
}

/// <summary>Response containing queue positions for orders.</summary>
public sealed record QueuePositionsResponse
{
    /// <summary>Queue positions returned by the API.</summary>
    [JsonPropertyName("queue_positions")]
    public IReadOnlyList<QueuePositionResponse> QueuePositions { get; init; } = [];
}
