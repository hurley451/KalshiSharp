using KalshiSharp.Http;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.CfBenchmarks;

internal sealed class CfBenchmarksClient(IKalshiHttpClient httpClient) : ICfBenchmarksClient
{
    private const string BasePath = "/trade-api/v2/cfbenchmarks";
    private readonly IKalshiHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <inheritdoc />
    public Task<CfBenchmarksResponse> GetCurrentAsync(
        string relativePath,
        IReadOnlyDictionary<string, string?>? queryParameters = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(relativePath, historical: false, queryParameters, cancellationToken);

    /// <inheritdoc />
    public Task<CfBenchmarksResponse> GetHistoricalAsync(
        string relativePath,
        IReadOnlyDictionary<string, string?>? queryParameters = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(relativePath, historical: true, queryParameters, cancellationToken);

    private Task<CfBenchmarksResponse> SendAsync(
        string relativePath,
        bool historical,
        IReadOnlyDictionary<string, string?>? queryParameters,
        CancellationToken cancellationToken)
    {
        var encodedPath = EncodeRelativePath(relativePath, historical);
        var historyPrefix = historical ? "/history" : string.Empty;

        return _httpClient.SendAsync<CfBenchmarksResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}{historyPrefix}/{encodedPath}",
            QueryParameters = queryParameters
        }, cancellationToken);
    }

    private static string EncodeRelativePath(string relativePath, bool historical)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        if (relativePath.Contains('\\', StringComparison.Ordinal) ||
            relativePath.Contains('?', StringComparison.Ordinal) ||
            relativePath.Contains('#', StringComparison.Ordinal) ||
            relativePath.Contains('%', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The provider path must be unescaped and cannot contain query, fragment, or backslash delimiters.",
                nameof(relativePath));
        }

        var segments = relativePath.Split('/', StringSplitOptions.None);
        if (segments.Any(segment =>
                string.IsNullOrWhiteSpace(segment) ||
                segment is "." or ".."))
        {
            throw new ArgumentException(
                "The provider path must contain non-empty segments without traversal.",
                nameof(relativePath));
        }

        if (string.Equals(segments[0], "history", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                historical
                    ? "Omit the history segment when calling GetHistoricalAsync."
                    : "Use GetHistoricalAsync for CF Benchmarks history endpoints.",
                nameof(relativePath));
        }

        return string.Join('/', segments.Select(Uri.EscapeDataString));
    }
}
