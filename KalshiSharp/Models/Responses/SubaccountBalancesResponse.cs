using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>Balances for all accessible subaccounts and exchange indexes.</summary>
public sealed record SubaccountBalancesResponse
{
    /// <summary>Subaccount balance entries.</summary>
    [JsonPropertyName("subaccount_balances")]
    public IReadOnlyList<SubaccountBalanceResponse> SubaccountBalances { get; init; } = [];
}

/// <summary>A balance entry for a subaccount and exchange index.</summary>
public sealed record SubaccountBalanceResponse
{
    /// <summary>Subaccount number.</summary>
    [JsonPropertyName("subaccount_number")]
    public required int SubaccountNumber { get; init; }

    /// <summary>Exchange shard index.</summary>
    [JsonPropertyName("exchange_index")]
    public required int ExchangeIndex { get; init; }

    /// <summary>Fixed-point dollar balance.</summary>
    [JsonPropertyName("balance")]
    public required string Balance { get; init; }

    /// <summary>Last update timestamp in seconds.</summary>
    [JsonPropertyName("updated_ts")]
    public required long UpdatedTs { get; init; }
}
