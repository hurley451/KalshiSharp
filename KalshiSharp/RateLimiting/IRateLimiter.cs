namespace KalshiSharp.RateLimiting;

/// <summary>
/// Abstraction for client-side rate limiting.
/// </summary>
public interface IRateLimiter : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Waits for permission to proceed with a request.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when permission is granted.</returns>
    ValueTask WaitAsync(CancellationToken cancellationToken = default);

    /// <summary>Waits for the token budget required by a classified request.</summary>
    ValueTask WaitAsync(RateLimitRequest request, CancellationToken cancellationToken = default) =>
        WaitAsync(cancellationToken);

    /// <summary>
    /// Gets whether rate limiting is currently active (tokens below threshold).
    /// </summary>
    bool IsThrottling { get; }
}
