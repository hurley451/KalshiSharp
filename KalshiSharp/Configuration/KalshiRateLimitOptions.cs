namespace KalshiSharp.Configuration;

/// <summary>Client-side token-budget configuration.</summary>
public sealed class KalshiRateLimitOptions
{
    /// <summary>Read tokens replenished per second.</summary>
    public int ReadTokensPerSecond { get; set; } = 100;

    /// <summary>Maximum read-token burst.</summary>
    public int ReadTokenLimit { get; set; } = 200;

    /// <summary>Write tokens replenished per second for each unscoped or shard bucket.</summary>
    public int WriteTokensPerSecond { get; set; } = 100;

    /// <summary>Maximum write-token burst for each bucket.</summary>
    public int WriteTokenLimit { get; set; } = 100;

    /// <summary>Maximum queued permits per bucket.</summary>
    public int QueueLimit { get; set; } = 100;

    /// <summary>Conservative cost used when an endpoint has no specific classification.</summary>
    public int DefaultTokenCost { get; set; } = 10;
}
