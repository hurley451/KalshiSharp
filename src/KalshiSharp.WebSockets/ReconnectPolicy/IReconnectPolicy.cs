namespace KalshiSharp.WebSockets.ReconnectPolicy;

/// <summary>
/// Defines a policy for WebSocket reconnection attempts.
/// </summary>
public interface IReconnectPolicy
{
    /// <summary>
    /// Gets the delay before the next reconnection attempt.
    /// </summary>
    /// <param name="attemptNumber">The current attempt number (1-based).</param>
    /// <returns>The delay before the next attempt, or null if no more attempts should be made.</returns>
    TimeSpan? GetNextDelay(int attemptNumber);

    /// <summary>
    /// Resets the policy state after a successful connection.
    /// </summary>
    void Reset();
}
