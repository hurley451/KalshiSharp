using System.Net;

namespace KalshiSharp.Core.Errors;

/// <summary>
/// Exception thrown when the rate limit is exceeded (429).
/// </summary>
public sealed class KalshiRateLimitException : KalshiException
{
    /// <summary>
    /// The time to wait before retrying, if provided by the API.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Creates a new instance of <see cref="KalshiRateLimitException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">The HTTP status code (429).</param>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="rawResponse">The raw response body.</param>
    /// <param name="requestId">The request ID for correlation.</param>
    /// <param name="retryAfter">The time to wait before retrying.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    public KalshiRateLimitException(
        string message,
        HttpStatusCode statusCode,
        string? errorCode = null,
        string? rawResponse = null,
        string? requestId = null,
        TimeSpan? retryAfter = null,
        Exception? innerException = null)
        : base(message, statusCode, errorCode, rawResponse, requestId, innerException)
    {
        RetryAfter = retryAfter;
    }
}
