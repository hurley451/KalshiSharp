using KalshiSharp.Http;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Communications;

internal sealed class CommunicationsClient(IKalshiHttpClient httpClient) : ICommunicationsClient
{
    private const string BasePath = "/trade-api/v2/communications";
    private readonly IKalshiHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <inheritdoc />
    public Task<CommunicationsIdResponse> GetCommunicationsIdAsync(CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<CommunicationsIdResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/id"
        }, cancellationToken);
}
