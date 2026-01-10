using System.Net;

namespace KalshiSharp.Core.Errors;

/// <summary>
/// Exception thrown when request validation fails (422).
/// </summary>
public sealed class KalshiValidationException : KalshiException
{
    /// <summary>
    /// Validation errors keyed by field name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    /// <summary>
    /// Creates a new instance of <see cref="KalshiValidationException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">The HTTP status code (422).</param>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="rawResponse">The raw response body.</param>
    /// <param name="requestId">The request ID for correlation.</param>
    /// <param name="validationErrors">Validation errors keyed by field name.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    public KalshiValidationException(
        string message,
        HttpStatusCode statusCode,
        string? errorCode = null,
        string? rawResponse = null,
        string? requestId = null,
        IReadOnlyDictionary<string, string[]>? validationErrors = null,
        Exception? innerException = null)
        : base(message, statusCode, errorCode, rawResponse, requestId, innerException)
    {
        ValidationErrors = validationErrors;
    }
}
