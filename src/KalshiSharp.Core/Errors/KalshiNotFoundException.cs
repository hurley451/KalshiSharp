using System.Net;

namespace KalshiSharp.Core.Errors;

/// <summary>
/// Exception thrown when a requested resource is not found (404).
/// </summary>
public sealed class KalshiNotFoundException : KalshiException
{
    /// <summary>
    /// Creates a new instance of <see cref="KalshiNotFoundException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">The HTTP status code (404).</param>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="rawResponse">The raw response body.</param>
    /// <param name="requestId">The request ID for correlation.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    public KalshiNotFoundException(
        string message,
        HttpStatusCode statusCode,
        string? errorCode = null,
        string? rawResponse = null,
        string? requestId = null,
        Exception? innerException = null)
        : base(message, statusCode, errorCode, rawResponse, requestId, innerException)
    {
    }
}
