using System.Text.Json.Serialization;
using KalshiSharp.Models.Common;
using KalshiSharp.Serialization.Converters;

namespace KalshiSharp.Models.Requests;

/// <summary>Filter value for communications records owned by the authenticated user.</summary>
public static class CommunicationsUserFilter
{
    /// <summary>Filter results to the authenticated user.</summary>
    public const string Self = "self";
}

/// <summary>Query parameters for listing RFQs.</summary>
public sealed record RfqQuery : PaginationParameters
{
    /// <summary>Filter by one event ticker.</summary>
    public string? EventTicker { get; init; }

    /// <summary>Filter by market ticker.</summary>
    public string? MarketTicker { get; init; }

    /// <summary>Filter by subaccount, including zero for the primary account.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Filter RFQs by status.</summary>
    public string? Status { get; init; }

    /// <summary>Deprecated creator user UUID filter. Prefer <see cref="UserFilter"/> for the authenticated user.</summary>
    public string? CreatorUserId { get; init; }

    /// <summary>Set to <c>self</c> to filter RFQs created by the authenticated user.</summary>
    public string? UserFilter { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        if (Limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(Limit), Limit, "Limit must be between 1 and 100.");
        }

        SettlementQuery.ValidateSubaccount(Subaccount);
        ValidateUserFilter(UserFilter);

        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.AppendIfNotEmpty("event_ticker", EventTicker);
        builder.AppendIfNotEmpty("market_ticker", MarketTicker);
        builder.AppendIfNotNull("subaccount", Subaccount);
        builder.AppendIfNotEmpty("status", Status);
        builder.AppendIfNotEmpty("creator_user_id", CreatorUserId);
        builder.AppendIfNotEmpty("user_filter", UserFilter);
        return builder.Build();
    }

    internal static void ValidateUserFilter(string? userFilter)
    {
        if (!string.IsNullOrEmpty(userFilter) &&
            !string.Equals(userFilter, CommunicationsUserFilter.Self, StringComparison.Ordinal))
        {
            throw new ArgumentException("UserFilter must be omitted or set to self.", nameof(userFilter));
        }
    }
}

/// <summary>Request for creating an RFQ.</summary>
public sealed record CreateRfqRequest
{
    /// <summary>Market ticker for the RFQ.</summary>
    public required string MarketTicker { get; init; }

    /// <summary>Whole-contract count for the RFQ.</summary>
    public int? Contracts { get; init; }

    /// <summary>Fixed-point contract count for the RFQ.</summary>
    public string? ContractsFp { get; init; }

    /// <summary>Deprecated target cost in centi-cents. Prefer <see cref="TargetCostDollars"/>.</summary>
    [Obsolete("Use TargetCostDollars instead.")]
    public long? TargetCostCentiCents { get; init; }

    /// <summary>Target cost in fixed-point dollars.</summary>
    public string? TargetCostDollars { get; init; }

    /// <summary>Whether target cost excludes taker fees.</summary>
    public bool? TargetCostExcludesFees { get; init; }

    /// <summary>Whether to obscure the creator's public communications ID from other users.</summary>
    public bool? ObscureCreatorId { get; init; }

    /// <summary>Whether to rest the remainder of the RFQ after execution.</summary>
    public required bool RestRemainder { get; init; }

    /// <summary>Whether to delete existing RFQs as part of creation.</summary>
    public bool? ReplaceExisting { get; init; }

    /// <summary>FCM subtrader to create the RFQ for.</summary>
    public string? SubtraderId { get; init; }

    /// <summary>Subaccount for the RFQ, including zero for the primary account.</summary>
    public int? Subaccount { get; init; }
}

/// <summary>Query parameters for listing quotes.</summary>
public sealed record QuoteQuery : PaginationParameters
{
    /// <summary>Only return quotes updated after this Unix timestamp.</summary>
    public DateTimeOffset? MinTime { get; init; }

    /// <summary>Only return quotes updated before this Unix timestamp.</summary>
    public DateTimeOffset? MaxTime { get; init; }

    /// <summary>Filter quotes by status.</summary>
    public string? Status { get; init; }

    /// <summary>Deprecated quote creator user UUID filter. Prefer <see cref="UserFilter"/> for the authenticated user.</summary>
    public string? QuoteCreatorUserId { get; init; }

    /// <summary>Set to <c>self</c> to filter quotes created by the authenticated user.</summary>
    public string? UserFilter { get; init; }

    /// <summary>Set to <c>self</c> to filter quotes responding to RFQs created by the authenticated user.</summary>
    public string? RfqUserFilter { get; init; }

    /// <summary>Deprecated RFQ creator user UUID filter. Prefer <see cref="RfqUserFilter"/> for the authenticated user.</summary>
    public string? RfqCreatorUserId { get; init; }

    /// <summary>FCM subtrader ID for the RFQ creator.</summary>
    public string? RfqCreatorSubtraderId { get; init; }

    /// <summary>Filter by RFQ UUID.</summary>
    public string? RfqId { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        if (Limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(Limit), Limit, "Limit must be between 1 and 500.");
        }

        RfqQuery.ValidateUserFilter(UserFilter);
        RfqQuery.ValidateUserFilter(RfqUserFilter);

        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.AppendIfNotNull("min_ts", MinTime?.ToUnixTimeSeconds());
        builder.AppendIfNotNull("max_ts", MaxTime?.ToUnixTimeSeconds());
        builder.AppendIfNotEmpty("status", Status);
        builder.AppendIfNotEmpty("quote_creator_user_id", QuoteCreatorUserId);
        builder.AppendIfNotEmpty("user_filter", UserFilter);
        builder.AppendIfNotEmpty("rfq_user_filter", RfqUserFilter);
        builder.AppendIfNotEmpty("rfq_creator_user_id", RfqCreatorUserId);
        builder.AppendIfNotEmpty("rfq_creator_subtrader_id", RfqCreatorSubtraderId);
        builder.AppendIfNotEmpty("rfq_id", RfqId);
        return builder.Build();
    }
}

/// <summary>Query parameters for listing block-trade proposals.</summary>
public sealed record BlockTradeProposalQuery : PaginationParameters
{
    /// <summary>Filter by market ticker.</summary>
    public string? MarketTicker { get; init; }

    /// <summary>Filter block-trade proposals by status.</summary>
    public string? Status { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        if (Limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(Limit), Limit, "Limit must be between 1 and 100.");
        }

        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.AppendIfNotEmpty("market_ticker", MarketTicker);
        builder.AppendIfNotEmpty("status", Status);
        return builder.Build();
    }
}

/// <summary>Request for proposing a block trade.</summary>
public sealed record ProposeBlockTradeRequest
{
    /// <summary>User ID of the buyer.</summary>
    public required string BuyerUserId { get; init; }

    /// <summary>User ID of the seller.</summary>
    public required string SellerUserId { get; init; }

    /// <summary>The ticker of the market for this block trade.</summary>
    public required string MarketTicker { get; init; }

    /// <summary>Price in centi-cents.</summary>
    public required long PriceCentiCents { get; init; }

    /// <summary>Number of contracts in centicounts.</summary>
    public required long Centicount { get; init; }

    /// <summary>The maker side of the trade. Valid values are <c>yes</c> and <c>no</c>.</summary>
    public required string MakerSide { get; init; }

    /// <summary>Expiration time of the proposal.</summary>
    [JsonConverter(typeof(IsoDateTimeOffsetConverter))]
    public required DateTimeOffset ExpirationTs { get; init; }

    /// <summary>Subtrader ID of the buyer. Provide either this or <see cref="BuyerSubaccount"/>, not both.</summary>
    public string? BuyerSubtraderId { get; init; }

    /// <summary>User-managed subaccount number of the buyer, including zero for primary.</summary>
    public int? BuyerSubaccount { get; init; }

    /// <summary>Subtrader ID of the seller. Provide either this or <see cref="SellerSubaccount"/>, not both.</summary>
    public string? SellerSubtraderId { get; init; }

    /// <summary>User-managed subaccount number of the seller, including zero for primary.</summary>
    public int? SellerSubaccount { get; init; }
}

/// <summary>Request for accepting a block-trade proposal.</summary>
public sealed record AcceptBlockTradeProposalRequest
{
    /// <summary>Subtrader ID to accept as. Provide either this or <see cref="Subaccount"/>, not both.</summary>
    public string? SubtraderId { get; init; }

    /// <summary>User-managed subaccount number to accept as, including zero for primary.</summary>
    public int? Subaccount { get; init; }
}

/// <summary>Request for creating a quote in response to an RFQ.</summary>
public sealed record CreateQuoteRequest
{
    /// <summary>RFQ UUID to quote on.</summary>
    public required string RfqId { get; init; }

    /// <summary>YES bid price in fixed-point dollars.</summary>
    public required string YesBid { get; init; }

    /// <summary>NO bid price in fixed-point dollars.</summary>
    public required string NoBid { get; init; }

    /// <summary>Whether to rest the remainder of the quote after execution.</summary>
    public required bool RestRemainder { get; init; }

    /// <summary>Whether to cancel instead of crossing if the quote would take liquidity.</summary>
    public bool? PostOnly { get; init; }

    /// <summary>Subaccount for the quote, including zero for the primary account.</summary>
    public int? Subaccount { get; init; }
}

/// <summary>Request for accepting a quote scoped to its RFQ.</summary>
public sealed record AcceptQuoteRequest
{
    /// <summary>The quote side to accept. Valid values are <c>yes</c> and <c>no</c>.</summary>
    public required string AcceptedSide { get; init; }
}
