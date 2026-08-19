using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>Authenticated account token-bucket limits.</summary>
public sealed record AccountApiLimitsResponse
{
    /// <summary>Effective Predictions API usage tier.</summary>
    [JsonPropertyName("usage_tier")]
    public required string UsageTier { get; init; }

    /// <summary>Read token bucket.</summary>
    [JsonPropertyName("read")]
    public required AccountTokenBucketResponse Read { get; init; }

    /// <summary>Write token bucket.</summary>
    [JsonPropertyName("write")]
    public required AccountTokenBucketResponse Write { get; init; }

    /// <summary>Active usage grants.</summary>
    [JsonPropertyName("grants")]
    public IReadOnlyList<AccountApiGrantResponse> Grants { get; init; } = [];
}

/// <summary>One server token-bucket budget.</summary>
public sealed record AccountTokenBucketResponse
{
    /// <summary>Tokens replenished per second.</summary>
    [JsonPropertyName("refill_rate")]
    public required int RefillRate { get; init; }

    /// <summary>Maximum token capacity.</summary>
    [JsonPropertyName("bucket_capacity")]
    public required int BucketCapacity { get; init; }
}

/// <summary>An active API usage grant.</summary>
public sealed record AccountApiGrantResponse
{
    /// <summary>Granted usage level.</summary>
    public required string Level { get; init; }

    /// <summary>Grant source.</summary>
    public string? Source { get; init; }

    /// <summary>Expiration timestamp in seconds.</summary>
    public long? ExpiresTs { get; init; }

    /// <summary>Exchange instance or lane when supplied.</summary>
    public int? ExchangeInstance { get; init; }
}

/// <summary>Current default and non-default endpoint token costs.</summary>
public sealed record EndpointCostsResponse
{
    /// <summary>Default token cost.</summary>
    [JsonPropertyName("default_cost")]
    public required int DefaultCost { get; init; }

    /// <summary>Endpoints whose cost differs from the default.</summary>
    [JsonPropertyName("endpoint_costs")]
    public IReadOnlyList<EndpointCostResponse> EndpointCosts { get; init; } = [];
}

/// <summary>Configured cost for one endpoint.</summary>
public sealed record EndpointCostResponse
{
    /// <summary>HTTP method.</summary>
    public required string Method { get; init; }

    /// <summary>API path.</summary>
    public required string Path { get; init; }

    /// <summary>Token cost.</summary>
    public required int Cost { get; init; }
}
