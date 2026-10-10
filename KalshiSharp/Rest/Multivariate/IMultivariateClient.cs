using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Multivariate;

/// <summary>Client for Kalshi multivariate event collection endpoints.</summary>
public interface IMultivariateClient
{
    /// <summary>Lists multivariate event collections with optional filters.</summary>
    /// <param name="query">Optional collection filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching multivariate event collections.</returns>
    Task<MultivariateEventCollectionsResponse> ListEventCollectionsAsync(
        MultivariateEventCollectionQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a multivariate event collection by ticker.</summary>
    /// <param name="collectionTicker">The collection ticker.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection, or <see langword="null"/> when a successful response omits it.</returns>
    Task<MultivariateEventCollectionResponse?> GetEventCollectionAsync(
        string collectionTicker,
        CancellationToken cancellationToken = default);
}
