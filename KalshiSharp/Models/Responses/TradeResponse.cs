using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents a trade that occurred on the exchange.
/// </summary>
public sealed record TradeResponse
{
    /// <summary>
    /// Unique identifier for this trade.
    /// </summary>
    public required string TradeId { get; init; }

    /// <summary>
    /// Market ticker this trade occurred in.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Side of the trade (Yes or No).
    /// </summary>
    public OrderSide Side { get; init; }

    /// <summary>
    /// Price at which the trade executed (in cents).
    /// </summary>
    public int YesPrice { get; init; }

    /// <summary>
    /// No price (derived from yes price).
    /// </summary>
    public int NoPrice { get; init; }

    /// <summary>
    /// Number of contracts traded.
    /// </summary>
    public int Count { get; init; }

    /// <summary>Number of contracts traded as a fixed-point count.</summary>
    public string? CountFp { get; init; }

    /// <summary>YES price in dollars.</summary>
    public string? YesPriceDollars { get; init; }

    /// <summary>NO price in dollars.</summary>
    public string? NoPriceDollars { get; init; }

    /// <summary>Whether this trade was a block trade.</summary>
    public bool IsBlockTrade { get; init; }

    /// <summary>
    /// When this trade occurred.
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>
    /// Taker side of the trade.
    /// </summary>
    public string? TakerSide { get; init; }
}
