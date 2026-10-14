using KalshiSharp.Models.Responses;
using KalshiSharp.Models.Requests;

namespace KalshiSharp.Rest.Communications;

/// <summary>
/// Client for Kalshi communications endpoints.
/// </summary>
public interface ICommunicationsClient
{
    /// <summary>Gets the caller's public communications ID.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The communications ID response.</returns>
    Task<CommunicationsIdResponse> GetCommunicationsIdAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists block-trade proposals visible to the caller.</summary>
    /// <param name="query">Optional block-trade proposal filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Block-trade proposals matching the query.</returns>
    Task<BlockTradeProposalsResponse> ListBlockTradeProposalsAsync(
        BlockTradeProposalQuery? query = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Creates a block-trade proposal.</summary>
    /// <param name="request">Block-trade proposal request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created block-trade proposal ID.</returns>
    Task<ProposeBlockTradeResponse> ProposeBlockTradeAsync(
        ProposeBlockTradeRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Accepts a block-trade proposal.</summary>
    /// <param name="blockTradeProposalId">Block-trade proposal ID.</param>
    /// <param name="request">Optional acceptance identity request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AcceptBlockTradeProposalAsync(
        string blockTradeProposalId,
        AcceptBlockTradeProposalRequest? request = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Lists RFQs visible to the caller.</summary>
    /// <param name="query">Optional RFQ filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>RFQs matching the query.</returns>
    Task<RfqsResponse> ListRfqsAsync(RfqQuery? query = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Creates an RFQ.</summary>
    /// <param name="request">RFQ creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created RFQ ID.</returns>
    Task<CreateRfqResponse> CreateRfqAsync(CreateRfqRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Gets one RFQ by ID.</summary>
    /// <param name="rfqId">RFQ ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested RFQ.</returns>
    Task<SingleRfqResponse> GetRfqAsync(string rfqId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Deletes one RFQ by ID.</summary>
    /// <param name="rfqId">RFQ ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteRfqAsync(string rfqId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Lists quotes visible to the caller.</summary>
    /// <param name="query">Optional quote filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Quotes matching the query.</returns>
    Task<QuotesResponse> ListQuotesAsync(QuoteQuery? query = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Creates a quote in response to an RFQ.</summary>
    /// <param name="request">Quote creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created quote ID.</returns>
    Task<CreateQuoteResponse> CreateQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Gets one quote scoped to its RFQ.</summary>
    /// <param name="rfqId">RFQ ID.</param>
    /// <param name="quoteId">Quote ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested quote.</returns>
    Task<SingleQuoteResponse> GetQuoteAsync(string rfqId, string quoteId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Deletes one quote scoped to its RFQ.</summary>
    /// <param name="rfqId">RFQ ID.</param>
    /// <param name="quoteId">Quote ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteQuoteAsync(string rfqId, string quoteId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Accepts one quote scoped to its RFQ.</summary>
    /// <param name="rfqId">RFQ ID.</param>
    /// <param name="quoteId">Quote ID.</param>
    /// <param name="request">Quote acceptance request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AcceptQuoteAsync(
        string rfqId,
        string quoteId,
        AcceptQuoteRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>Confirms one accepted quote scoped to its RFQ.</summary>
    /// <param name="rfqId">RFQ ID.</param>
    /// <param name="quoteId">Quote ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConfirmQuoteAsync(string rfqId, string quoteId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
