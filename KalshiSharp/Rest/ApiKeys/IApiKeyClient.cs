using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.ApiKeys;

/// <summary>Client for Kalshi API-key administration endpoints.</summary>
public interface IApiKeyClient
{
    /// <summary>Lists API keys associated with the authenticated user.</summary>
    Task<ApiKeysResponse> ListApiKeysAsync(
        ApiKeyQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates an API key using caller-managed public key material.</summary>
    Task<CreateApiKeyResponse> CreateApiKeyAsync(
        CreateApiKeyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an API key and one-time private key material. Store the private key immediately.
    /// </summary>
    Task<GenerateApiKeyResponse> GenerateApiKeyAsync(
        GenerateApiKeyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes an API key by ID.</summary>
    Task DeleteApiKeyAsync(string apiKeyId, CancellationToken cancellationToken = default);
}
