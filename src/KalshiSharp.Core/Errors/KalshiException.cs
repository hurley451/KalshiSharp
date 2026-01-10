using System.Net;

namespace KalshiSharp.Core.Errors;

/// <summary>
/// Base exception for all Kalshi API errors.
/// </summary>
public class KalshiException : Exception
{
    /// <summary>
    /// The HTTP status code returned by the API.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// The error code returned by the API, if available.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// The raw response body, if available.
    /// </summary>
    public string? RawResponse { get; }

    /// <summary>
    /// The request ID for correlation, if available.
    /// </summary>
    public string? RequestId { get; }

    /// <summary>
    /// Creates a new instance of <see cref="KalshiException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="rawResponse">The raw response body.</param>
    /// <param name="requestId">The request ID for correlation.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    public KalshiException(
        string message,
        HttpStatusCode statusCode,
        string? errorCode = null,
        string? rawResponse = null,
        string? requestId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        RawResponse = rawResponse;
        RequestId = requestId;
    }

    /// <summary>
    /// Creates a new instance of <see cref="KalshiException"/> from an error response.
    /// </summary>
    internal static KalshiException FromResponse(
        HttpStatusCode statusCode,
        KalshiErrorResponse? errorResponse,
        string? rawResponse,
        string? requestId)
    {
        var message = errorResponse?.Message ?? $"Kalshi API error: {(int)statusCode} {statusCode}";
        var errorCode = errorResponse?.Code;

        return statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new KalshiAuthException(message, statusCode, errorCode, rawResponse, requestId),

            HttpStatusCode.NotFound =>
                new KalshiNotFoundException(message, statusCode, errorCode, rawResponse, requestId),

            HttpStatusCode.UnprocessableEntity =>
                new KalshiValidationException(
                    message,
                    statusCode,
                    errorCode,
                    rawResponse,
                    requestId,
                    errorResponse?.Errors?.AsReadOnly()),

            HttpStatusCode.TooManyRequests =>
                new KalshiRateLimitException(message, statusCode, errorCode, rawResponse, requestId, retryAfter: null),

            _ => new KalshiException(message, statusCode, errorCode, rawResponse, requestId)
        };
    }
}

/// <summary>
/// Extension methods for dictionary to convert to read-only.
/// </summary>
internal static class DictionaryExtensions
{
    public static IReadOnlyDictionary<string, string[]>? AsReadOnly(this Dictionary<string, string[]>? dict)
        => dict is null ? null : new Dictionary<string, string[]>(dict);
}
