using System.Text.Json.Serialization;

namespace KalshiSharp.Core.Errors;

/// <summary>
/// Represents the error response structure returned by the Kalshi API.
/// </summary>
internal sealed record KalshiErrorResponse
{
    /// <summary>
    /// The error code returned by the API.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>
    /// The error message returned by the API.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// Validation errors keyed by field name.
    /// </summary>
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; init; }
}
