using System.Text.Json.Serialization;
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

    /// <summary>Resolved market outcome, including scalar settlements.</summary>
    [JsonPropertyName("market_result")]
    public SettlementOutcome? Outcome { get; init; }

    /// <summary>Compatibility projection for binary outcomes. Scalar settlements return null; use <see cref="Outcome"/> for the complete result.</summary>
    [JsonIgnore]
    public OrderSide? MarketResult
    {
        get => Outcome switch { SettlementOutcome.Yes => OrderSide.Yes, SettlementOutcome.No => OrderSide.No, _ => null };
        init => Outcome = value switch
        {
            OrderSide.Yes => SettlementOutcome.Yes,
            OrderSide.No => SettlementOutcome.No,
            null => null,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported binary settlement outcome.")
        };
    }

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

    /// <summary>Payout per single YES contract, in cents. This is not the total settlement revenue.</summary>
    public long? Value { get; init; }
}
