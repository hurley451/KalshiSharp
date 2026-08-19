namespace KalshiSharp.Configuration;

/// <summary>
/// Configuration options for the Kalshi API client.
/// </summary>
public sealed class KalshiClientOptions
{
    private static readonly Uri ProductionUri = new("https://api.elections.kalshi.com");
    private static readonly Uri DemoUri = new("https://demo-api.kalshi.co");

    /// <summary>
    /// The API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The API secret for request signing.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// The target environment. Defaults to <see cref="KalshiEnvironment.Production"/>.
    /// </summary>
    public KalshiEnvironment Environment { get; set; } = KalshiEnvironment.Production;

    /// <summary>
    /// Optional base URI override. If not specified, derived from <see cref="Environment"/>.
    /// </summary>
    public Uri? BaseUri { get; set; }

    /// <summary>
    /// HTTP request timeout. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Tolerance for clock skew between client and server. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan ClockSkewTolerance { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to enable client-side rate limiting. Defaults to true.
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>Token-budget configuration for read, write, and shard-specific write buckets.</summary>
    public KalshiRateLimitOptions RateLimits { get; } = new();

    /// <summary>
    /// Gets the effective base URI, using <see cref="BaseUri"/> if specified,
    /// otherwise deriving from <see cref="Environment"/>.
    /// </summary>
    /// <returns>The base URI to use for API requests.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="Environment"/> is invalid.</exception>
    public Uri GetEffectiveBaseUri() => BaseUri ?? Environment switch
    {
        KalshiEnvironment.Production => ProductionUri,
        KalshiEnvironment.Demo => DemoUri,
        _ => throw new ArgumentOutOfRangeException(nameof(Environment), Environment, "Invalid environment value.")
    };
}
