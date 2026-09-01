using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Milestones;

internal sealed class MilestoneClient(IKalshiHttpClient httpClient) : IMilestoneClient
{
    private const string BasePath = "/trade-api/v2/milestones";
    private readonly IKalshiHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <inheritdoc />
    public async Task<MilestoneResponse> GetMilestoneAsync(
        string milestoneId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(milestoneId);

        var response = await _httpClient.SendAsync<SingleMilestoneResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{Uri.EscapeDataString(milestoneId)}"
        }, cancellationToken).ConfigureAwait(false);

        return response.Milestone;
    }

    /// <inheritdoc />
    public Task<MilestonesResponse> ListMilestonesAsync(
        MilestoneQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _httpClient.SendAsync<MilestonesResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}{query.ToQueryString()}"
        }, cancellationToken);
    }
}
