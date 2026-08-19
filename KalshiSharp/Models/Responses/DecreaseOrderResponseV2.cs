namespace KalshiSharp.Models.Responses;

/// <summary>Response returned after decreasing a V2 event order.</summary>
public sealed record DecreaseOrderResponseV2
{
    /// <summary>Unique identifier of the decreased order.</summary>
    public required string OrderId { get; init; }

    /// <summary>Number of contracts remaining after the decrease.</summary>
    public required string RemainingCount { get; init; }

    /// <summary>Matching-engine processing timestamp in Unix milliseconds.</summary>
    public required long TsMs { get; init; }

    /// <summary>Client-provided order identifier, when present.</summary>
    public string? ClientOrderId { get; init; }
}
