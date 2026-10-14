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
    public Task<BlockTradeProposalsResponse> ListBlockTradeProposalsAsync(
        BlockTradeProposalQuery? query = null,
        CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<BlockTradeProposalsResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/block-trade-proposals{query?.ToQueryString()}"
        }, cancellationToken);

    /// <inheritdoc />
    public Task<ProposeBlockTradeResponse> ProposeBlockTradeAsync(
        ProposeBlockTradeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateProposeBlockTradeRequest(request);

        return _httpClient.SendAsync<ProposeBlockTradeResponse>(new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = $"{BasePath}/block-trade-proposals",
            Content = request
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AcceptBlockTradeProposalAsync(
        string blockTradeProposalId,
        AcceptBlockTradeProposalRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ValidateAcceptBlockTradeProposalRequest(request);

        return _httpClient.SendAsync(new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = $"{BasePath}/block-trade-proposals/{EscapeId(blockTradeProposalId, nameof(blockTradeProposalId))}/accept",
            Content = request
        }, cancellationToken);
    }

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

    /// <inheritdoc />
    public Task<SingleQuoteResponse> GetQuoteAsync(string rfqId, string quoteId, CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<SingleQuoteResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = QuotePath(rfqId, quoteId)
        }, cancellationToken);

    /// <inheritdoc />
    public Task DeleteQuoteAsync(string rfqId, string quoteId, CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync(new KalshiRequest
        {
            Method = HttpMethod.Delete,
            Path = QuotePath(rfqId, quoteId)
        }, cancellationToken);

    /// <inheritdoc />
    public Task AcceptQuoteAsync(
        string rfqId,
        string quoteId,
        AcceptQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAcceptQuoteRequest(request);

        return _httpClient.SendAsync(new KalshiRequest
        {
            Method = HttpMethod.Put,
            Path = $"{QuotePath(rfqId, quoteId)}/accept",
            Content = request
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConfirmQuoteAsync(string rfqId, string quoteId, CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync(new KalshiRequest
        {
            Method = HttpMethod.Put,
            Path = $"{QuotePath(rfqId, quoteId)}/confirm"
        }, cancellationToken);

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

    private static void ValidateAcceptQuoteRequest(AcceptQuoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(request.AcceptedSide, "yes", StringComparison.Ordinal) &&
            !string.Equals(request.AcceptedSide, "no", StringComparison.Ordinal))
        {
            throw new ArgumentException("AcceptedSide must be yes or no.", nameof(request));
        }
    }

    private static void ValidateProposeBlockTradeRequest(ProposeBlockTradeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ThrowIfNullOrWhiteSpace(request.BuyerUserId, nameof(request.BuyerUserId));
        ThrowIfNullOrWhiteSpace(request.SellerUserId, nameof(request.SellerUserId));
        ThrowIfNullOrWhiteSpace(request.MarketTicker, nameof(request.MarketTicker));
        ValidatePositive(request.PriceCentiCents, nameof(request.PriceCentiCents));
        ValidatePositive(request.Centicount, nameof(request.Centicount));
        ValidateYesNo(request.MakerSide, nameof(request.MakerSide));
        ValidateSubtraderOrSubaccount(request.BuyerSubtraderId, request.BuyerSubaccount, "buyer");
        ValidateSubtraderOrSubaccount(request.SellerSubtraderId, request.SellerSubaccount, "seller");
    }

    private static void ValidateAcceptBlockTradeProposalRequest(AcceptBlockTradeProposalRequest? request)
    {
        if (request is null)
        {
            return;
        }

        ValidateSubtraderOrSubaccount(request.SubtraderId, request.Subaccount, "acceptance");
    }

    private static string QuotePath(string rfqId, string quoteId) =>
        $"{BasePath}/rfqs/{EscapeId(rfqId, nameof(rfqId))}/quotes/{EscapeId(quoteId, nameof(quoteId))}";

    private static void ValidatePositive(long value, string parameterName)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to 1.");
        }
    }

    private static void ValidateYesNo(string? value, string parameterName)
    {
        if (!string.Equals(value, "yes", StringComparison.Ordinal) &&
            !string.Equals(value, "no", StringComparison.Ordinal))
        {
            throw new ArgumentException("Value must be yes or no.", parameterName);
        }
    }

    private static void ValidateSubtraderOrSubaccount(string? subtraderId, int? subaccount, string name)
    {
        if (!string.IsNullOrWhiteSpace(subtraderId) && subaccount is not null)
        {
            throw new ArgumentException($"Provide either {name} subtrader ID or {name} subaccount, not both.", name);
        }

        SettlementQuery.ValidateSubaccount(subaccount);
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
