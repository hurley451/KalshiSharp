using KalshiSharp.Models.Common;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Responses;

/// <summary>Response for listing portfolio settlements.</summary>
public sealed record SettlementsResponse : PagedResponse<SettlementResponse>
{
    /// <summary>Settlement records.</summary>
    public IReadOnlyList<SettlementResponse> Settlements { get; init; } = [];

    /// <inheritdoc />
    public override IReadOnlyList<SettlementResponse> Items => Settlements;
}

/// <summary>A settled market position entry.</summary>
public sealed record SettlementResponse
{
    /// <summary>Market ticker.</summary>
    public required string Ticker { get; init; }

    /// <summary>Exchange shard where the settled market traded.</summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>Event ticker.</summary>
    public string? EventTicker { get; init; }

    /// <summary>Resolved market outcome.</summary>
    public OrderSide? MarketResult { get; init; }

    /// <summary>YES position count as a fixed-point quantity.</summary>
    public string? YesCountFp { get; init; }

    /// <summary>Total YES cost in fixed-point dollars.</summary>
    public string? YesTotalCostDollars { get; init; }

    /// <summary>NO position count as a fixed-point quantity.</summary>
    public string? NoCountFp { get; init; }

    /// <summary>Total NO cost in fixed-point dollars.</summary>
    public string? NoTotalCostDollars { get; init; }

    /// <summary>Settlement revenue, in cents.</summary>
    public long? Revenue { get; init; }

    /// <summary>Settlement timestamp.</summary>
    public DateTimeOffset? SettledTime { get; init; }

    /// <summary>Fees paid in fixed-point dollars.</summary>
    public string? FeeCost { get; init; }

    /// <summary>Settlement value, in cents.</summary>
    public long? Value { get; init; }
}
