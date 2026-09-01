using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.StructuredTargets;

internal sealed class StructuredTargetClient(IKalshiHttpClient httpClient) : IStructuredTargetClient
{
    private const string BasePath = "/trade-api/v2/structured_targets";
    private readonly IKalshiHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <inheritdoc />
    public async Task<StructuredTargetResponse?> GetStructuredTargetAsync(
        string structuredTargetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(structuredTargetId);

        var response = await _httpClient.SendAsync<SingleStructuredTargetResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{Uri.EscapeDataString(structuredTargetId)}"
        }, cancellationToken).ConfigureAwait(false);

        return response.StructuredTarget;
    }

    /// <inheritdoc />
    public Task<StructuredTargetsResponse> ListStructuredTargetsAsync(
        StructuredTargetQuery? query = null,
        CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<StructuredTargetsResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}{query?.ToQueryString()}"
        }, cancellationToken);
}
