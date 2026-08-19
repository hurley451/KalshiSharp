using KalshiSharp.Http;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Account;

internal sealed class AccountClient(IKalshiHttpClient httpClient) : IAccountClient
{
    private const string BasePath = "/trade-api/v2/account";
    private readonly IKalshiHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <inheritdoc />
    public Task<AccountApiLimitsResponse> GetApiLimitsAsync(CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<AccountApiLimitsResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/limits"
        }, cancellationToken);

    /// <inheritdoc />
    public Task<EndpointCostsResponse> GetEndpointCostsAsync(CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<EndpointCostsResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/endpoint_costs"
        }, cancellationToken);
}
