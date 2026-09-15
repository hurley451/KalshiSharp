namespace KalshiSharp.Models.Requests;

/// <summary>Optional filters for listing API keys.</summary>
public sealed record ApiKeyQuery
{
    /// <summary>
    /// FCM subtrader ID used to filter keys bound to a subtrader.
    /// </summary>
    public string? FcmSubtraderId { get; init; }

    /// <summary>Converts the query to a URL query string.</summary>
    public string ToQueryString()
    {
        if (string.IsNullOrWhiteSpace(FcmSubtraderId))
        {
            return string.Empty;
        }

        return $"?fcm_subtrader_id={Uri.EscapeDataString(FcmSubtraderId)}";
    }
}

/// <summary>Request for creating an API key with caller-managed public key material.</summary>
public sealed record CreateApiKeyRequest
{
    /// <summary>Human-readable API key name.</summary>
    public required string Name { get; init; }

    /// <summary>RSA public key in PEM format.</summary>
    public required string PublicKey { get; init; }

    /// <summary>Optional API key scopes. Omit for Kalshi's default full access.</summary>
    public IReadOnlyList<string>? Scopes { get; init; }

    /// <summary>Optional subaccount binding.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Optional FCM subtrader binding.</summary>
    public string? FcmSubtraderId { get; init; }
}

/// <summary>Request for generating a Kalshi-managed API key pair.</summary>
public sealed record GenerateApiKeyRequest
{
    /// <summary>Human-readable API key name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional API key scopes. Omit for Kalshi's default full access.</summary>
    public IReadOnlyList<string>? Scopes { get; init; }

    /// <summary>Optional subaccount binding.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Optional FCM subtrader binding.</summary>
    public string? FcmSubtraderId { get; init; }
}
