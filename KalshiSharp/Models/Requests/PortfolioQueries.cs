using System.Globalization;
using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Query parameters for portfolio settlement history.</summary>
public sealed record SettlementQuery : PaginationParameters
{
    /// <summary>Filter by market ticker.</summary>
    public string? Ticker { get; init; }

    /// <summary>Filter by one event ticker.</summary>
    public string? EventTicker { get; init; }

    /// <summary>Filter settlements after this Unix timestamp.</summary>
    public DateTimeOffset? MinTime { get; init; }

    /// <summary>Filter settlements before this Unix timestamp.</summary>
    public DateTimeOffset? MaxTime { get; init; }

    /// <summary>Filter by subaccount, including zero for the primary account. Omitted returns all subaccounts.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        ValidateLimit(Limit);
        ValidateSubaccount(Subaccount);

        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.AppendIfNotEmpty("ticker", Ticker);
        builder.AppendIfNotEmpty("event_ticker", EventTicker);
        builder.AppendIfNotNull("min_ts", MinTime?.ToUnixTimeSeconds());
        builder.AppendIfNotNull("max_ts", MaxTime?.ToUnixTimeSeconds());
        builder.AppendIfNotNull("subaccount", Subaccount);
        return builder.Build();
    }

    internal static void ValidateLimit(int? limit)
    {
        if (limit is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be between 1 and 1000.");
        }
    }

    internal static void ValidateSubaccount(int? subaccount)
    {
        if (subaccount is < 0 or > 63)
        {
            throw new ArgumentOutOfRangeException(
                nameof(subaccount),
                subaccount,
                "Subaccount must be between 0 and 63.");
        }
    }
}

/// <summary>Query parameters for subaccount transfer history.</summary>
public sealed record SubaccountTransferQuery : PaginationParameters
{
    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        SettlementQuery.ValidateLimit(Limit);

        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        return builder.Build();
    }
}

/// <summary>Request for creating a numbered subaccount.</summary>
public sealed record CreateSubaccountRequest
{
    /// <summary>Exchange shard for the new subaccount. Defaults to 0 when omitted.</summary>
    public int? ExchangeIndex { get; init; }
}

/// <summary>Request for transferring funds between subaccounts.</summary>
public sealed record CreateSubaccountTransferRequest
{
    /// <summary>Client-generated UUID used to make transfer requests idempotent.</summary>
    public required Guid ClientTransferId { get; init; }

    /// <summary>Source subaccount. Use 0 for the primary account.</summary>
    public required int FromSubaccount { get; init; }

    /// <summary>Destination subaccount. Use 0 for the primary account.</summary>
    public required int ToSubaccount { get; init; }

    /// <summary>Amount to transfer, in cents.</summary>
    public required long AmountCents { get; init; }

    /// <summary>Exchange shard to apply the transfer on. Defaults to 0 when omitted.</summary>
    public int? ExchangeIndex { get; init; }
}

/// <summary>Request for updating subaccount netting.</summary>
public sealed record UpdateSubaccountNettingRequest
{
    /// <summary>Subaccount number. Use 0 for the primary account.</summary>
    public required int SubaccountNumber { get; init; }

    /// <summary>Whether netting is enabled for the subaccount.</summary>
    public required bool Enabled { get; init; }
}
