using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents a market on the Kalshi exchange.
/// </summary>
public sealed record MarketResponse
{
    /// <summary>
    /// Unique ticker identifier for this market.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Event ticker this market belongs to.
    /// </summary>
    public required string EventTicker { get; init; }

    /// <summary>
    /// Human-readable title/question for this market.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Subtitle providing additional context.
    /// </summary>
    public string? Subtitle { get; init; }

    /// <summary>
    /// Current status of the market.
    /// </summary>
    public required MarketStatus Status { get; init; }

    /// <summary>
    /// Current yes price (0-100 cents).
    /// </summary>
    public required decimal YesBid { get; init; }

    /// <summary>
    /// Current yes ask price (0-100 cents).
    /// </summary>
    public required decimal YesAsk { get; init; }

    /// <summary>
    /// Current no bid price (0-100 cents).
    /// </summary>
    public required decimal NoBid { get; init; }

    /// <summary>
    /// Current no ask price (0-100 cents).
    /// </summary>
    public required decimal NoAsk { get; init; }

    /// <summary>
    /// Last traded price.
    /// </summary>
    public decimal? LastPrice { get; init; }

    /// <summary>
    /// Previous day's closing price.
    /// </summary>
    public decimal? PreviousYesBid { get; init; }

    /// <summary>
    /// Previous day's closing price.
    /// </summary>
    public decimal? PreviousPrice { get; init; }

    /// <summary>
    /// Total volume traded in this market.
    /// </summary>
    public required int Volume { get; init; }

    /// <summary>
    /// 24-hour trading volume.
    /// </summary>
    public required int Volume24H { get; init; }

    /// <summary>
    /// Current open interest (total outstanding contracts).
    /// </summary>
    public required int OpenInterest { get; init; }

    /// <summary>
    /// When the market closes for trading.
    /// </summary>
    public DateTimeOffset? CloseTime { get; init; }

    /// <summary>
    /// When the market expires/settles.
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; init; }

    /// <summary>
    /// The settlement result if market is settled (Yes/No).
    /// </summary>
    public string? Result { get; init; }

    /// <summary>
    /// Whether this market can be traded.
    /// </summary>
    public required bool CanCloseEarly { get; init; }

    /// <summary>
    /// Category of this market.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Risk limit per member in cents.
    /// </summary>
    public int? RiskLimitCents { get; init; }

    /// <summary>
    /// Strike value for numeric markets.
    /// </summary>
    public decimal? StrikeValue { get; init; }

    /// <summary>
    /// Floor strike for ranged markets.
    /// </summary>
    public decimal? FloorStrike { get; init; }

    /// <summary>
    /// Cap strike for ranged markets.
    /// </summary>
    public decimal? CapStrike { get; init; }
}
