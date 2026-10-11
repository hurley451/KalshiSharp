using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
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

    /// <inheritdoc />
    public Task<RfqsResponse> ListRfqsAsync(RfqQuery? query = null, CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<RfqsResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/rfqs{query?.ToQueryString()}"
        }, cancellationToken);

    /// <inheritdoc />
    public Task<CreateRfqResponse> CreateRfqAsync(CreateRfqRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateRfqRequest(request);

        return _httpClient.SendAsync<CreateRfqResponse>(new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = $"{BasePath}/rfqs",
            Content = request
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SingleRfqResponse> GetRfqAsync(string rfqId, CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<SingleRfqResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/rfqs/{EscapeId(rfqId, nameof(rfqId))}"
        }, cancellationToken);

    /// <inheritdoc />
    public Task DeleteRfqAsync(string rfqId, CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync(new KalshiRequest
        {
            Method = HttpMethod.Delete,
            Path = $"{BasePath}/rfqs/{EscapeId(rfqId, nameof(rfqId))}"
        }, cancellationToken);

    /// <inheritdoc />
    public Task<QuotesResponse> ListQuotesAsync(QuoteQuery? query = null, CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<QuotesResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/quotes{query?.ToQueryString()}"
        }, cancellationToken);

    /// <inheritdoc />
    public Task<CreateQuoteResponse> CreateQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateQuoteRequest(request);

        return _httpClient.SendAsync<CreateQuoteResponse>(new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = $"{BasePath}/quotes",
            Content = request
        }, cancellationToken);
    }

    private static void ValidateCreateRfqRequest(CreateRfqRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ThrowIfNullOrWhiteSpace(request.MarketTicker, nameof(request.MarketTicker));
        SettlementQuery.ValidateSubaccount(request.Subaccount);
    }

    private static void ValidateCreateQuoteRequest(CreateQuoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ThrowIfNullOrWhiteSpace(request.RfqId, nameof(request.RfqId));
        ThrowIfNullOrWhiteSpace(request.YesBid, nameof(request.YesBid));
        ThrowIfNullOrWhiteSpace(request.NoBid, nameof(request.NoBid));
        SettlementQuery.ValidateSubaccount(request.Subaccount);
    }

    private static string EscapeId(string id, string parameterName)
    {
        ThrowIfNullOrWhiteSpace(id, parameterName);
        return Uri.EscapeDataString(id);
    }

    private static void ThrowIfNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
    }
}
