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
    public required OrderSide Side { get; init; }

    /// <summary>
    /// Price at which the trade executed (in cents).
    /// </summary>
    public required int YesPrice { get; init; }

    /// <summary>
    /// No price (derived from yes price).
    /// </summary>
    public required int NoPrice { get; init; }

    /// <summary>
    /// Number of contracts traded.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// When this trade occurred.
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>
    /// Taker side of the trade.
    /// </summary>
    public string? TakerSide { get; init; }
}
