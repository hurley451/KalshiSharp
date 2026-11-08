using KalshiSharp.Models.Common;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Responses;

/// <summary>Response for listing portfolio credit history.</summary>
public sealed record CreditHistoryResponse : PagedResponse<CreditHistoryEntry>
{
    /// <summary>Credit records, ordered newest first.</summary>
    public required IReadOnlyList<CreditHistoryEntry> Credits { get; init; }

    /// <inheritdoc />
    public override IReadOnlyList<CreditHistoryEntry> Items => Credits;
}

/// <summary>A credit applied, pending, or failed for the authenticated member.</summary>
public sealed record CreditHistoryEntry
{
    /// <summary>Unique credit identifier.</summary>
    public required string CreditId { get; init; }

    /// <summary>Credit category supplied by Kalshi.</summary>
    public required string Type { get; init; }

    /// <summary>Credit processing status.</summary>
    public required CreditStatus Status { get; init; }

    /// <summary>Credit amount as a fixed-point dollar string.</summary>
    public required string AmountDollars { get; init; }

    /// <summary>Credit description, which can include a market or event ticker.</summary>
    public required string Reason { get; init; }

    /// <summary>Time the credit was created.</summary>
    public required DateTimeOffset CreatedTime { get; init; }
}
