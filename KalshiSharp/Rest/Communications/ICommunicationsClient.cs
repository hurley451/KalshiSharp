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
}
