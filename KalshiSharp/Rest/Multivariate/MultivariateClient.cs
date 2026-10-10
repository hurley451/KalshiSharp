using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Multivariate;

internal sealed class MultivariateClient(IKalshiHttpClient httpClient) : IMultivariateClient
{
    private const string BasePath = "/trade-api/v2/multivariate_event_collections";
    private readonly IKalshiHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <inheritdoc />
    public Task<MultivariateEventCollectionsResponse> ListEventCollectionsAsync(
        MultivariateEventCollectionQuery? query = null,
        CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<MultivariateEventCollectionsResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}{query?.ToQueryString()}"
        }, cancellationToken);

    /// <inheritdoc />
    public async Task<MultivariateEventCollectionResponse?> GetEventCollectionAsync(
        string collectionTicker,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionTicker);

        var response = await _httpClient.SendAsync<SingleMultivariateEventCollectionResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{Uri.EscapeDataString(collectionTicker)}"
        }, cancellationToken).ConfigureAwait(false);

        return response.MultivariateContract;
    }
}
