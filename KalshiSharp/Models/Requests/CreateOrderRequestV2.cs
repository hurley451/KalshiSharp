using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Request to create a new order via the V2 events orders endpoint.
/// </summary>
public sealed record CreateOrderRequestV2
{
    /// <summary>
    /// The market ticker to place the order on.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Side of the book. For event markets, this refers to the YES leg only:
    /// bid means buy YES, ask means sell YES.
    /// </summary>
    public required OrderBookSide Side { get; init; }

    /// <summary>
    /// String representation of the order quantity in contracts (e.g. "10.00").
    /// </summary>
    public required string Count { get; init; }

    /// <summary>
    /// Price for the order in fixed-point dollars (e.g. "0.5600").
    /// </summary>
    public required string Price { get; init; }

    /// <summary>
    /// Specifies how long the order remains active.
    /// Use <see cref="TimeInForce.GoodTillCanceled"/> with <see cref="ExpirationTime"/> for a GTT order.
    /// <see cref="TimeInForce.ImmediateOrCancel"/> cannot be combined with <see cref="ExpirationTime"/>.
    /// </summary>
    public required TimeInForce TimeInForce { get; init; }

    /// <summary>
    /// The self-trade prevention strategy for this order.
    /// </summary>
    public required SelfTradePreventionType SelfTradePreventionType { get; init; }

    /// <summary>
    /// Optional client-provided order ID for correlation.
    /// </summary>
    public string? ClientOrderId { get; init; }

    /// <summary>
    /// Optional Unix timestamp in seconds for when the order expires.
    /// Only valid when <see cref="TimeInForce"/> is <see cref="TimeInForce.GoodTillCanceled"/>.
    /// </summary>
    public long? ExpirationTime { get; init; }

    /// <summary>
    /// If true, the order will only be placed if it can rest on the book (maker only).
    /// </summary>
    public bool? PostOnly { get; init; }

    /// <summary>
    /// If true, the order will be canceled when trading on the exchange is paused.
    /// </summary>
    public bool? CancelOrderOnPause { get; init; }

    /// <summary>
    /// If true, the placed count is capped by the member's current position.
    /// </summary>
    public bool? ReduceOnly { get; init; }

    /// <summary>
    /// The subaccount number to use for this order. 0 is the primary subaccount.
    /// </summary>
    public int? Subaccount { get; init; }

    /// <summary>
    /// The order group this order is part of.
    /// </summary>
    public string? OrderGroupId { get; init; }

    /// <summary>
    /// Identifier for an exchange shard. Defaults to 0 if unspecified.
    /// </summary>
    public int? ExchangeIndex { get; init; }
}
