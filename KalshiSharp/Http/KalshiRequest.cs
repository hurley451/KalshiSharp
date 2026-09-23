namespace KalshiSharp.Http;

/// <summary>
/// Represents a request to the Kalshi API.
/// </summary>
public sealed class KalshiRequest
{
    /// <summary>
    /// The HTTP method for the request.
    /// </summary>
    public required HttpMethod Method { get; init; }

    /// <summary>
    /// The relative path for the request (e.g., "/trade-api/v2/markets").
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Optional query parameters to append to the URL.
    /// </summary>
    public IReadOnlyDictionary<string, string?>? QueryParameters { get; init; }

    /// <summary>
    /// Optional request body content (will be serialized as JSON).
    /// </summary>
    public object? Content { get; init; }

    /// <summary>
    /// Whether successful response content should be redacted from deserialization exceptions.
    /// </summary>
    public bool RedactResponseContentInExceptions { get; init; }

    /// <summary>
    /// Builds the full relative URI including query parameters.
    /// </summary>
    /// <returns>The relative URI with query string.</returns>
    public string BuildRelativeUri()
    {
        if (QueryParameters is null || QueryParameters.Count == 0)
        {
            return Path;
        }

        var queryPairs = QueryParameters
            .Where(kvp => kvp.Value is not null)
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}");

        var queryString = string.Join("&", queryPairs);
        return string.IsNullOrEmpty(queryString) ? Path : $"{Path}?{queryString}";
    }
}
