namespace KalshiSharp.Configuration;

/// <summary>Client-side token-budget configuration.</summary>
public sealed class KalshiRateLimitOptions
{
    /// <summary>Read tokens replenished per second.</summary>
    public int ReadTokensPerSecond { get; set; } = 100;

    /// <summary>Maximum read-token burst.</summary>
    public int ReadTokenLimit { get; set; } = 200;

    /// <summary>Portfolio balance reads replenished per second. Kalshi's Basic and Advanced tiers allow one.</summary>
    public int BalanceTokensPerSecond { get; set; } = 1;

    /// <summary>Maximum portfolio-balance read burst. Kalshi allows no burst.</summary>
    public int BalanceTokenLimit { get; set; } = 1;

    /// <summary>Write tokens replenished per second for each unscoped or shard bucket.</summary>
    public int WriteTokensPerSecond { get; set; } = 100;

    /// <summary>Maximum write-token burst for each bucket.</summary>
    public int WriteTokenLimit { get; set; } = 100;

    /// <summary>Maximum queued permits per bucket.</summary>
    public int QueueLimit { get; set; } = 100;

    /// <summary>Conservative cost used when an endpoint has no specific classification.</summary>
    public int DefaultTokenCost { get; set; } = 10;
}
