namespace KalshiSharp.Models.Responses;

/// <summary>Response returned after creating a subaccount.</summary>
public sealed record CreateSubaccountResponse
{
    /// <summary>Assigned subaccount number.</summary>
    public required int SubaccountNumber { get; init; }
}

/// <summary>Response for subaccount netting settings.</summary>
public sealed record SubaccountNettingResponse
{
    /// <summary>Netting configuration entries.</summary>
    public IReadOnlyList<SubaccountNettingConfig> NettingConfigs { get; init; } = [];
}

/// <summary>Netting configuration for one subaccount and exchange index.</summary>
public sealed record SubaccountNettingConfig
{
    /// <summary>Subaccount number.</summary>
    public required int SubaccountNumber { get; init; }

    /// <summary>Whether netting is enabled.</summary>
    public required bool Enabled { get; init; }

    /// <summary>Exchange shard index.</summary>
    public required int ExchangeIndex { get; init; }
}
