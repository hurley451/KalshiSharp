using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Incentives;

internal sealed class IncentiveClient(IKalshiHttpClient httpClient) : IIncentiveClient
{
    private const string BasePath = "/trade-api/v2/incentive_programs";
    private readonly IKalshiHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <inheritdoc />
    public Task<IncentiveProgramsResponse> ListIncentiveProgramsAsync(
        IncentiveProgramQuery? query = null,
        CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<IncentiveProgramsResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}{query?.ToQueryString()}"
        }, cancellationToken);
}
