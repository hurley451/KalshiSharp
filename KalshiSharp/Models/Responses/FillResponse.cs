using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents a fill (partial or complete execution of an order).
/// </summary>
public sealed record FillResponse
{
    /// <summary>Unique fill identifier.</summary>
    public string? FillId { get; init; }

    /// <summary>
    /// Unique identifier for this fill.
    /// </summary>
    public required string TradeId { get; init; }

    /// <summary>
    /// Order ID this fill belongs to.
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// Market ticker for this fill.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Side of the order (Yes or No).
    /// </summary>
    public OrderSide Side { get; init; }

    /// <summary>
    /// Action (buy or sell).
    /// </summary>
    public string? Action { get; init; }

    /// <summary>
    /// Number of contracts filled.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Yes price at which the fill executed (in cents).
    /// </summary>
    public int YesPrice { get; init; }

    /// <summary>
    /// No price at which the fill executed (in cents).
    /// </summary>
    public int NoPrice { get; init; }

    /// <summary>Market ticker under the current fill contract.</summary>
    public string? MarketTicker { get; init; }

    /// <summary>Filled quantity as a fixed-point count.</summary>
    public string? CountFp { get; init; }

    /// <summary>YES execution price in dollars.</summary>
    public string? YesPriceDollars { get; init; }

    /// <summary>NO execution price in dollars.</summary>
    public string? NoPriceDollars { get; init; }

    /// <summary>Fee cost in dollars.</summary>
    public string? FeeCost { get; init; }

    /// <summary>Canonical outcome side, when supplied.</summary>
    public OrderSide? OutcomeSide { get; init; }

    /// <summary>Canonical book side, when supplied.</summary>
    public OrderBookSide? BookSide { get; init; }

    /// <summary>Matching-engine timestamp.</summary>
    public long? Ts { get; init; }

    /// <summary>Subaccount that owns the fill.</summary>
    public int? SubaccountNumber { get; init; }

    /// <summary>Exchange shard where the fill occurred.</summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>
    /// Whether this was a maker or taker fill.
    /// </summary>
    public required bool IsTaker { get; init; }

    /// <summary>
    /// When this fill occurred.
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }
}
