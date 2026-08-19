namespace KalshiSharp.RateLimiting;

/// <summary>Classification used for one client-side rate-limit acquisition.</summary>
public sealed record RateLimitRequest
{
    /// <summary>Whether the request consumes the write budget.</summary>
    public required bool IsWrite { get; init; }

    /// <summary>Number of server rate-limit tokens consumed.</summary>
    public required int TokenCost { get; init; }

    /// <summary>Explicit positive exchange index for a shard-local write bucket.</summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>Whether this is a batch request, which always consumes the unscoped bucket.</summary>
    public bool IsBatch { get; init; }
}
