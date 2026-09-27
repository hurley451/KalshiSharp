using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Responses;

/// <summary>Response for listing subaccount transfer history.</summary>
public sealed record SubaccountTransfersResponse : PagedResponse<SubaccountTransferResponse>
{
    /// <summary>Subaccount transfer records.</summary>
    public IReadOnlyList<SubaccountTransferResponse> Transfers { get; init; } = [];

    /// <inheritdoc />
    public override IReadOnlyList<SubaccountTransferResponse> Items => Transfers;
}

/// <summary>A transfer between two subaccounts.</summary>
public sealed record SubaccountTransferResponse
{
    /// <summary>Kalshi transfer identifier.</summary>
    public required string TransferId { get; init; }

    /// <summary>Source subaccount.</summary>
    public required int FromSubaccount { get; init; }

    /// <summary>Destination subaccount.</summary>
    public required int ToSubaccount { get; init; }

    /// <summary>Transferred amount, in cents.</summary>
    public required long AmountCents { get; init; }

    /// <summary>Creation timestamp in Unix seconds.</summary>
    public required long CreatedTs { get; init; }

    /// <summary>Exchange shard where the transfer was applied.</summary>
    public required int ExchangeIndex { get; init; }
}
