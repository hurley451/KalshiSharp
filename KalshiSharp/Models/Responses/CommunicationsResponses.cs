using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response containing the caller's public communications ID.
/// </summary>
public sealed record CommunicationsIdResponse
{
    /// <summary>Public communications ID used to identify the user in RFQ and quote workflows.</summary>
    public required string CommunicationsId { get; init; }
}

/// <summary>Response for creating an RFQ.</summary>
public sealed record CreateRfqResponse
{
    /// <summary>UUID of the newly created RFQ.</summary>
    public required string Id { get; init; }
}

/// <summary>Response for listing RFQs.</summary>
public sealed record RfqsResponse : PagedResponse<RfqResponse>
{
    /// <summary>RFQs in the current page.</summary>
    public IReadOnlyList<RfqResponse> Rfqs { get; init; } = [];

    /// <inheritdoc />
    public override IReadOnlyList<RfqResponse> Items => Rfqs;
}

/// <summary>Response for retrieving one RFQ.</summary>
public sealed record SingleRfqResponse
{
    /// <summary>The requested RFQ.</summary>
    public required RfqResponse Rfq { get; init; }
}

/// <summary>One request for quote.</summary>
public sealed record RfqResponse
{
    /// <summary>RFQ UUID.</summary>
    public required string Id { get; init; }

    /// <summary>Public communications ID of the RFQ creator.</summary>
    public required string CreatorId { get; init; }

    /// <summary>Market ticker for the RFQ.</summary>
    public required string MarketTicker { get; init; }

    /// <summary>Fixed-point contract count requested.</summary>
    public required string ContractsFp { get; init; }

    /// <summary>Target cost in fixed-point dollars.</summary>
    public string? TargetCostDollars { get; init; }

    /// <summary>Whether target cost excludes taker fees.</summary>
    public bool? TargetCostExcludesFees { get; init; }

    /// <summary>Current RFQ status.</summary>
    public required string Status { get; init; }

    /// <summary>Time the RFQ was created.</summary>
    public required DateTimeOffset CreatedTs { get; init; }

    /// <summary>Multivariate collection ticker, when present.</summary>
    public string? MveCollectionTicker { get; init; }

    /// <summary>Selected multivariate legs.</summary>
    public IReadOnlyList<RfqSelectedLeg> MveSelectedLegs { get; init; } = [];

    /// <summary>Whether to rest the remainder of the RFQ after execution.</summary>
    public bool? RestRemainder { get; init; }

    /// <summary>Reason for RFQ cancellation, when present.</summary>
    public string? CancellationReason { get; init; }

    /// <summary>Private user ID of the RFQ creator, when visible.</summary>
    public string? CreatorUserId { get; init; }

    /// <summary>Subaccount of the RFQ creator, when visible.</summary>
    public int? CreatorSubaccount { get; init; }

    /// <summary>Time the RFQ was cancelled, when present.</summary>
    public DateTimeOffset? CancelledTs { get; init; }

    /// <summary>Time the RFQ was last updated, when present.</summary>
    public DateTimeOffset? UpdatedTs { get; init; }
}

/// <summary>One selected leg in a multivariate RFQ.</summary>
public sealed record RfqSelectedLeg
{
    /// <summary>Selected event ticker.</summary>
    public string? EventTicker { get; init; }

    /// <summary>Selected market ticker.</summary>
    public string? MarketTicker { get; init; }

    /// <summary>Selected side.</summary>
    public string? Side { get; init; }

    /// <summary>YES settlement value in fixed-point dollars, when available.</summary>
    public string? YesSettlementValueDollars { get; init; }
}

/// <summary>Response for creating a quote.</summary>
public sealed record CreateQuoteResponse
{
    /// <summary>UUID of the newly created quote.</summary>
    public required string Id { get; init; }
}

/// <summary>Response for listing quotes.</summary>
public sealed record QuotesResponse : PagedResponse<QuoteResponse>
{
    /// <summary>Quotes in the current page.</summary>
    public IReadOnlyList<QuoteResponse> Quotes { get; init; } = [];

    /// <inheritdoc />
    public override IReadOnlyList<QuoteResponse> Items => Quotes;
}

/// <summary>Response for retrieving one quote.</summary>
public sealed record SingleQuoteResponse
{
    /// <summary>The requested quote.</summary>
    public required QuoteResponse Quote { get; init; }
}

/// <summary>One quote responding to an RFQ.</summary>
public sealed record QuoteResponse
{
    /// <summary>Quote UUID.</summary>
    public required string Id { get; init; }

    /// <summary>RFQ UUID this quote responds to.</summary>
    public required string RfqId { get; init; }

    /// <summary>Public communications ID of the quote creator.</summary>
    public required string CreatorId { get; init; }

    /// <summary>Public communications ID of the RFQ creator.</summary>
    public required string RfqCreatorId { get; init; }

    /// <summary>Market ticker for the quote.</summary>
    public required string MarketTicker { get; init; }

    /// <summary>Fixed-point contract count in the quote.</summary>
    public required string ContractsFp { get; init; }

    /// <summary>YES bid price in fixed-point dollars.</summary>
    public required string YesBidDollars { get; init; }

    /// <summary>NO bid price in fixed-point dollars.</summary>
    public required string NoBidDollars { get; init; }

    /// <summary>Time the quote was created.</summary>
    public required DateTimeOffset CreatedTs { get; init; }

    /// <summary>Time the quote was last updated.</summary>
    public required DateTimeOffset UpdatedTs { get; init; }

    /// <summary>Current quote status.</summary>
    public required string Status { get; init; }

    /// <summary>Accepted side, when accepted.</summary>
    public string? AcceptedSide { get; init; }

    /// <summary>Time the quote was accepted, when present.</summary>
    public DateTimeOffset? AcceptedTs { get; init; }

    /// <summary>Time the quote was confirmed, when present.</summary>
    public DateTimeOffset? ConfirmedTs { get; init; }

    /// <summary>Time the quote was executed, when present.</summary>
    public DateTimeOffset? ExecutedTs { get; init; }

    /// <summary>Time the quote was cancelled, when present.</summary>
    public DateTimeOffset? CancelledTs { get; init; }

    /// <summary>Whether to rest the remainder of the quote after execution.</summary>
    public bool? RestRemainder { get; init; }

    /// <summary>Whether the quote creator's order is post-only, when visible.</summary>
    public bool? PostOnly { get; init; }

    /// <summary>Reason for quote cancellation, when present.</summary>
    public string? CancellationReason { get; init; }

    /// <summary>Private user ID of the quote creator, when visible.</summary>
    public string? CreatorUserId { get; init; }

    /// <summary>Private user ID of the RFQ creator, when visible.</summary>
    public string? RfqCreatorUserId { get; init; }

    /// <summary>RFQ target cost in fixed-point dollars.</summary>
    public string? RfqTargetCostDollars { get; init; }

    /// <summary>Whether the RFQ target cost excludes taker fees.</summary>
    public bool? TargetCostExcludesFees { get; init; }

    /// <summary>Private order ID for the RFQ creator, when visible.</summary>
    public string? RfqCreatorOrderId { get; init; }

    /// <summary>Private order ID for the quote creator, when visible.</summary>
    public string? CreatorOrderId { get; init; }

    /// <summary>Subaccount of the quote creator, when visible.</summary>
    public int? CreatorSubaccount { get; init; }

    /// <summary>Subaccount of the RFQ creator, when visible.</summary>
    public int? RfqCreatorSubaccount { get; init; }

    /// <summary>YES contracts offered in fixed-point count format.</summary>
    public string? YesContractsFp { get; init; }

    /// <summary>NO contracts offered in fixed-point count format.</summary>
    public string? NoContractsFp { get; init; }
}
