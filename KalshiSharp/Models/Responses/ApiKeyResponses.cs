namespace KalshiSharp.Models.Responses;

/// <summary>Response returned by the API-key listing endpoint.</summary>
public sealed record ApiKeysResponse
{
    /// <summary>API keys associated with the authenticated user.</summary>
    public required IReadOnlyList<ApiKeyResponse> ApiKeys { get; init; }

    /// <summary>
    /// Unix timestamp, in seconds, when API-key location attestation expires.
    /// </summary>
    public long? ApiKeyRegionExpirationTs { get; init; }
}

/// <summary>API key metadata.</summary>
public sealed record ApiKeyResponse
{
    /// <summary>API key ID.</summary>
    public required string ApiKeyId { get; init; }

    /// <summary>Human-readable API key name.</summary>
    public required string Name { get; init; }

    /// <summary>Granted scopes.</summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>Optional subaccount binding.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Optional FCM subtrader binding.</summary>
    public string? FcmSubtraderId { get; init; }
}

/// <summary>Response returned after registering caller-managed public key material.</summary>
public sealed record CreateApiKeyResponse
{
    /// <summary>Created API key ID.</summary>
    public required string ApiKeyId { get; init; }

    /// <summary>Optional server warning.</summary>
    public string? Warning { get; init; }
}

/// <summary>Response returned after generating a Kalshi-managed key pair.</summary>
public sealed record GenerateApiKeyResponse
{
    /// <summary>Generated API key ID.</summary>
    public required string ApiKeyId { get; init; }

    /// <summary>
    /// One-time private key material. Store it immediately; Kalshi cannot return it again.
    /// </summary>
    public required string PrivateKey { get; init; }

    /// <summary>Optional server warning.</summary>
    public string? Warning { get; init; }

    /// <summary>Returns a redacted description so private-key material is not leaked accidentally.</summary>
    public override string ToString() =>
        $"{nameof(GenerateApiKeyResponse)} {{ {nameof(ApiKeyId)} = {ApiKeyId}, {nameof(PrivateKey)} = <redacted>, {nameof(Warning)} = {Warning ?? string.Empty} }}";
}
