using KalshiSharp.Http;
using KalshiSharp.Models.Common;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.ApiKeys;

internal sealed class ApiKeyClient(IKalshiHttpClient httpClient) : IApiKeyClient
{
    private const string BasePath = "/trade-api/v2/api_keys";
    private readonly IKalshiHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <inheritdoc />
    public Task<ApiKeysResponse> ListApiKeysAsync(
        ApiKeyQuery? query = null,
        CancellationToken cancellationToken = default) =>
        _httpClient.SendAsync<ApiKeysResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}{query?.ToQueryString()}"
        }, cancellationToken);

    /// <inheritdoc />
    public Task<CreateApiKeyResponse> CreateApiKeyAsync(
        CreateApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateKeyName(request.Name);
        ValidateRequiredText(request.PublicKey, nameof(request.PublicKey));
        ValidateRestriction(request.Subaccount, request.FcmSubtraderId);
        ValidateScopes(request.Scopes);

        return _httpClient.SendAsync<CreateApiKeyResponse>(new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = BasePath,
            Content = request
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<GenerateApiKeyResponse> GenerateApiKeyAsync(
        GenerateApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateKeyName(request.Name);
        ValidateRestriction(request.Subaccount, request.FcmSubtraderId);
        ValidateScopes(request.Scopes);

        return _httpClient.SendAsync<GenerateApiKeyResponse>(new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = $"{BasePath}/generate",
            Content = request,
            RedactResponseContentInExceptions = true
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteApiKeyAsync(string apiKeyId, CancellationToken cancellationToken = default)
    {
        ValidateRequiredText(apiKeyId, nameof(apiKeyId));

        return _httpClient.SendAsync(new KalshiRequest
        {
            Method = HttpMethod.Delete,
            Path = $"{BasePath}/{Uri.EscapeDataString(apiKeyId)}"
        }, cancellationToken);
    }

    private static void ValidateKeyName(string name) => ValidateRequiredText(name, nameof(name));

    private static void ValidateRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", parameterName);
        }
    }

    private static void ValidateRestriction(int? subaccount, string? fcmSubtraderId)
    {
        if (subaccount is < 0 or > 63)
        {
            throw new ArgumentOutOfRangeException(nameof(subaccount), subaccount, "Subaccount must be between 0 and 63.");
        }

        if (subaccount.HasValue && !string.IsNullOrWhiteSpace(fcmSubtraderId))
        {
            throw new ArgumentException("Subaccount and FCM subtrader bindings are mutually exclusive.", nameof(fcmSubtraderId));
        }
    }

    private static void ValidateScopes(IReadOnlyList<string>? scopes)
    {
        if (scopes is null)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scope in scopes)
        {
            if (string.IsNullOrWhiteSpace(scope) || !KnownScopes.Contains(scope))
            {
                throw new ArgumentException("API key scopes must be documented Kalshi scope values.", nameof(scopes));
            }

            if (!seen.Add(scope))
            {
                throw new ArgumentException("API key scopes cannot contain duplicates.", nameof(scopes));
            }
        }

        if (seen.Contains(ApiKeyScopes.Write) && !seen.Contains(ApiKeyScopes.Read))
        {
            throw new ArgumentException("API key scopes that include broad write access must also include broad read access.", nameof(scopes));
        }
    }

    private static readonly HashSet<string> KnownScopes = new(StringComparer.Ordinal)
    {
        ApiKeyScopes.Read,
        ApiKeyScopes.Write,
        ApiKeyScopes.ReadBlockTradeAccept,
        ApiKeyScopes.ReadPortfolioBalance,
        ApiKeyScopes.WriteTrade,
        ApiKeyScopes.WriteTransfer,
        ApiKeyScopes.WriteBlockTradeAccept
    };
}
