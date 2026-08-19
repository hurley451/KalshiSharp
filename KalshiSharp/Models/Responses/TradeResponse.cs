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

    /// <summary>Canonical outcome side of the taker.</summary>
    public OrderSide? TakerOutcomeSide { get; init; }

    /// <summary>Canonical order-book side of the taker.</summary>
    public OrderBookSide? TakerBookSide { get; init; }
}
