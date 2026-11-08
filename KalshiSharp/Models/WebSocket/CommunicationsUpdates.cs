using System.Text.Json.Serialization;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.WebSocket;

/// <summary>Notification that an RFQ was created.</summary>
public sealed record RfqCreatedUpdate : WebSocketMessage<RfqCreatedUpdate.MessageBody>
{
    /// <inheritdoc />
    public override string Type => "rfq_created";

    /// <summary>RFQ creation payload.</summary>
    public sealed record MessageBody
    {
        public required string Id { get; init; }
        public required string CreatorId { get; init; }
        public required string MarketTicker { get; init; }
        public string? EventTicker { get; init; }
        public string? ContractsFp { get; init; }
        public DateTimeOffset? CreatedTs { get; init; }
    }
}

/// <summary>Notification that an RFQ was closed.</summary>
public sealed record RfqDeletedUpdate : WebSocketMessage<RfqDeletedUpdate.MessageBody>
{
    /// <inheritdoc />
    public override string Type => "rfq_deleted";

    /// <summary>RFQ closure payload.</summary>
    public sealed record MessageBody
    {
        public required string Id { get; init; }
        public required string CreatorId { get; init; }
        public required string MarketTicker { get; init; }
        public string? EventTicker { get; init; }
        public string? ContractsFp { get; init; }
        public string? TargetCostDollars { get; init; }
        public DateTimeOffset? DeletedTs { get; init; }
    }
}

/// <summary>Notification that a quote was created.</summary>
public sealed record QuoteCreatedUpdate : WebSocketMessage<QuoteCreatedUpdate.MessageBody>
{
    /// <inheritdoc />
    public override string Type => "quote_created";

    /// <summary>Quote creation payload.</summary>
    public sealed record MessageBody : QuoteMessageBody
    {
        public DateTimeOffset? CreatedTs { get; init; }
    }
}

/// <summary>Notification that a quote was accepted.</summary>
public sealed record QuoteAcceptedUpdate : WebSocketMessage<QuoteAcceptedUpdate.MessageBody>
{
    /// <inheritdoc />
    public override string Type => "quote_accepted";

    /// <summary>Quote acceptance payload.</summary>
    public sealed record MessageBody : QuoteMessageBody
    {
        public OrderSide? AcceptedSide { get; init; }
        public string? ContractsAcceptedFp { get; init; }
    }
}

/// <summary>Notification that an accepted quote was executed.</summary>
public sealed record QuoteExecutedUpdate : WebSocketMessage<QuoteExecutedUpdate.MessageBody>
{
    /// <inheritdoc />
    public override string Type => "quote_executed";

    /// <summary>Quote execution payload.</summary>
    public sealed record MessageBody
    {
        public required string QuoteId { get; init; }
        public required string RfqId { get; init; }
        public required string QuoteCreatorId { get; init; }
        public required string RfqCreatorId { get; init; }
        public string? OrderId { get; init; }
        public string? ClientOrderId { get; init; }
        public required string MarketTicker { get; init; }
        public DateTimeOffset? ExecutedTs { get; init; }
        public int? Subaccount { get; init; }
    }
}

/// <summary>Fields common to quote-created and quote-accepted messages.</summary>
public abstract record QuoteMessageBody
{
    public required string QuoteId { get; init; }
    public required string RfqId { get; init; }
    public required string QuoteCreatorId { get; init; }
    public required string RfqCreatorId { get; init; }
    public required string MarketTicker { get; init; }
    public string? EventTicker { get; init; }
    public string? YesBidDollars { get; init; }
    public string? NoBidDollars { get; init; }
    public string? YesContractsOfferedFp { get; init; }
    public string? NoContractsOfferedFp { get; init; }
    public string? RfqTargetCostDollars { get; init; }
    public int? Subaccount { get; init; }
}
