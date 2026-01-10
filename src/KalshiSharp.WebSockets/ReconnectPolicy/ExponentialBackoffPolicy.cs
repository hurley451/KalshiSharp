namespace KalshiSharp.WebSockets.ReconnectPolicy;

/// <summary>
/// Implements exponential backoff reconnection policy.
/// Default: 1s, 2s, 4s, 8s, ... up to max 30s.
/// </summary>
public sealed class ExponentialBackoffPolicy : IReconnectPolicy
{
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _maxDelay;
    private readonly int? _maxAttempts;
    private readonly double _multiplier;
    private readonly Random _jitterRandom;

    /// <summary>
    /// Gets the initial delay between reconnection attempts.
    /// </summary>
    public TimeSpan InitialDelay => _initialDelay;

    /// <summary>
    /// Gets the maximum delay between reconnection attempts.
    /// </summary>
    public TimeSpan MaxDelay => _maxDelay;

    /// <summary>
    /// Gets the maximum number of reconnection attempts, or null for unlimited.
    /// </summary>
    public int? MaxAttempts => _maxAttempts;

    /// <summary>
    /// Initializes a new instance of <see cref="ExponentialBackoffPolicy"/> with default settings.
    /// </summary>
    /// <remarks>
    /// Default: 1s initial, 30s max, unlimited attempts, 2x multiplier.
    /// </remarks>
    public ExponentialBackoffPolicy()
        : this(
            initialDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(30),
            maxAttempts: null,
            multiplier: 2.0)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ExponentialBackoffPolicy"/> with custom settings.
    /// </summary>
    /// <param name="initialDelay">Initial delay before first retry.</param>
    /// <param name="maxDelay">Maximum delay between attempts.</param>
    /// <param name="maxAttempts">Maximum number of attempts, or null for unlimited.</param>
    /// <param name="multiplier">Multiplier for each subsequent delay (default 2.0).</param>
    public ExponentialBackoffPolicy(
        TimeSpan initialDelay,
        TimeSpan maxDelay,
        int? maxAttempts = null,
        double multiplier = 2.0)
    {
        if (initialDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialDelay), "Initial delay must be positive.");
        }

        if (maxDelay < initialDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDelay), "Max delay must be >= initial delay.");
        }

        if (maxAttempts is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be positive if specified.");
        }

        if (multiplier <= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Multiplier must be > 1.0.");
        }

        _initialDelay = initialDelay;
        _maxDelay = maxDelay;
        _maxAttempts = maxAttempts;
        _multiplier = multiplier;
        _jitterRandom = new Random();
    }

    /// <inheritdoc />
    public TimeSpan? GetNextDelay(int attemptNumber)
    {
        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be positive.");
        }

        // Check max attempts
        if (_maxAttempts.HasValue && attemptNumber > _maxAttempts.Value)
        {
            return null;
        }

        // Calculate exponential delay: initialDelay * multiplier^(attempt-1)
        // For attempt 1: 1s, attempt 2: 2s, attempt 3: 4s, attempt 4: 8s, etc.
        var exponent = attemptNumber - 1;
        var delayMs = _initialDelay.TotalMilliseconds * Math.Pow(_multiplier, exponent);

        // Cap at max delay
        delayMs = Math.Min(delayMs, _maxDelay.TotalMilliseconds);

        // Add jitter (±10% of delay)
        var jitterRange = delayMs * 0.1;
        var jitter = (_jitterRandom.NextDouble() * 2 - 1) * jitterRange;
        delayMs = Math.Max(0, delayMs + jitter);

        return TimeSpan.FromMilliseconds(delayMs);
    }

    /// <inheritdoc />
    public void Reset()
    {
        // No state to reset in this implementation
        // The attempt number is passed in externally
    }

    /// <summary>
    /// Creates a policy with default Kalshi settings (1s, 2s, 4s, 8s, max 30s, unlimited attempts).
    /// </summary>
    public static ExponentialBackoffPolicy Default => new();

    /// <summary>
    /// Creates a policy with finite attempts.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of reconnection attempts.</param>
    public static ExponentialBackoffPolicy WithMaxAttempts(int maxAttempts) =>
        new(
            initialDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(30),
            maxAttempts: maxAttempts);
}
