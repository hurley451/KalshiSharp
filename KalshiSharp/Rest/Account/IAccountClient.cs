using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Account;

/// <summary>Client for account usage and rate-limit discovery.</summary>
public interface IAccountClient
{
    /// <summary>Gets the authenticated account's token-bucket limits.</summary>
    Task<AccountApiLimitsResponse> GetApiLimitsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the current non-default endpoint token costs.</summary>
    Task<EndpointCostsResponse> GetEndpointCostsAsync(CancellationToken cancellationToken = default);
}
