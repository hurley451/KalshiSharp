using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents the user's account balance.
/// </summary>
public sealed record BalanceResponse
{
    /// <summary>
    /// Member's available balance in cents. This represents the amount available for trading.
    /// </summary>
    public required long Balance { get; init; }

    /// <summary>Member's available balance as a fixed-point dollar string.</summary>
    public string? BalanceDollars { get; init; }

    /// <summary>
    /// Member's portfolio value in cents. This is the current value of all positions held.
    /// </summary>
    public required long PortfolioValue { get; init; }

    /// <summary>
    /// Unix timestamp of the last update to the balance.
    /// </summary>
    public required long UpdatedTs { get; init; }

    /// <summary>Balance amounts broken down by exchange index.</summary>
    [JsonPropertyName("balance_breakdown")]
    public IReadOnlyList<ExchangeBalanceResponse> BalanceBreakdown { get; init; } = [];
}

/// <summary>A balance amount for one exchange index.</summary>
public sealed record ExchangeBalanceResponse
{
    /// <summary>Exchange shard index.</summary>
    [JsonPropertyName("exchange_index")]
    public required int ExchangeIndex { get; init; }

    /// <summary>Fixed-point dollar balance.</summary>
    [JsonPropertyName("balance")]
    public required string Balance { get; init; }
}
