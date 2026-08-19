using System.Text.Json.Serialization;
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
    /// Type of market (e.g., "binary").
    /// </summary>
    public string? MarketType { get; init; }

    /// <summary>
    /// Exchange shard that owns this market.
    /// </summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>
    /// Current yes bid price (0-100 cents).
    /// </summary>
    public int YesBid { get; init; }

    /// <summary>
    /// Current yes ask price (0-100 cents).
    /// </summary>
    public int YesAsk { get; init; }

    /// <summary>
    /// Current no bid price (0-100 cents).
    /// </summary>
    public int NoBid { get; init; }

    /// <summary>
    /// Current no ask price (0-100 cents).
    /// </summary>
    public int NoAsk { get; init; }

    /// <summary>
    /// Last traded price in cents.
    /// </summary>
    public int? LastPrice { get; init; }

    /// <summary>
    /// Previous day's yes ask price.
    /// </summary>
    public int? PreviousYesAsk { get; init; }

    /// <summary>
    /// Previous day's yes bid price.
    /// </summary>
    public int? PreviousYesBid { get; init; }

    /// <summary>
    /// Previous day's price.
    /// </summary>
    public int? PreviousPrice { get; init; }

    /// <summary>
    /// Total volume traded in this market.
    /// </summary>
    public int Volume { get; init; }

    /// <summary>
    /// 24-hour trading volume.
    /// </summary>
    [JsonPropertyName("volume_24h")]
    public int Volume24H { get; init; }

    /// <summary>
    /// Current open interest (total outstanding contracts).
    /// </summary>
    public int OpenInterest { get; init; }

    /// <summary>
    /// Current liquidity in cents.
    /// </summary>
    public int? Liquidity { get; init; }

    /// <summary>
    /// Notional value in cents.
    /// </summary>
    public int? NotionalValue { get; init; }

    /// <summary>Current YES bid price in dollars.</summary>
    public string? YesBidDollars { get; init; }

    /// <summary>Current YES bid size as a fixed-point count.</summary>
    public string? YesBidSizeFp { get; init; }

    /// <summary>Current YES ask price in dollars.</summary>
    public string? YesAskDollars { get; init; }

    /// <summary>Current YES ask size as a fixed-point count.</summary>
    public string? YesAskSizeFp { get; init; }

    /// <summary>Current NO bid price in dollars.</summary>
    public string? NoBidDollars { get; init; }

    /// <summary>Current NO ask price in dollars.</summary>
    public string? NoAskDollars { get; init; }

    /// <summary>Last traded price in dollars.</summary>
    public string? LastPriceDollars { get; init; }

    /// <summary>Total volume as a fixed-point count.</summary>
    public string? VolumeFp { get; init; }

    /// <summary>24-hour volume as a fixed-point count.</summary>
    [JsonPropertyName("volume_24h_fp")]
    public string? Volume24HFp { get; init; }

    /// <summary>Open interest as a fixed-point count.</summary>
    public string? OpenInterestFp { get; init; }

    /// <summary>Current liquidity in dollars.</summary>
    public string? LiquidityDollars { get; init; }

    /// <summary>Notional value in dollars.</summary>
    public string? NotionalValueDollars { get; init; }

    /// <summary>Previous YES bid price in dollars.</summary>
    public string? PreviousYesBidDollars { get; init; }

    /// <summary>Previous YES ask price in dollars.</summary>
    public string? PreviousYesAskDollars { get; init; }

    /// <summary>Previous traded price in dollars.</summary>
    public string? PreviousPriceDollars { get; init; }

    /// <summary>Settlement value in dollars.</summary>
    public string? SettlementValueDollars { get; init; }

    /// <summary>Current price-level structure identifier.</summary>
    public string? PriceLevelStructure { get; init; }

    /// <summary>Dynamic price ranges supported by the market.</summary>
    public IReadOnlyList<PriceRange> PriceRanges { get; init; } = [];

    /// <summary>
    /// Tick size for price increments.
    /// </summary>
    public int? TickSize { get; init; }

    /// <summary>
    /// When the market opens for trading.
    /// </summary>
    public DateTimeOffset? OpenTime { get; init; }

    /// <summary>
    /// When the market closes for trading.
    /// </summary>
    public DateTimeOffset? CloseTime { get; init; }

    /// <summary>
    /// When the market expires/settles.
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; init; }

    /// <summary>
    /// Expected expiration time.
    /// </summary>
    public DateTimeOffset? ExpectedExpirationTime { get; init; }

    /// <summary>
    /// Latest possible expiration time.
    /// </summary>
    public DateTimeOffset? LatestExpirationTime { get; init; }

    /// <summary>
    /// When the market was created.
    /// </summary>
    public DateTimeOffset? CreatedTime { get; init; }

    /// <summary>When the market was last updated.</summary>
    public DateTimeOffset? UpdatedTime { get; init; }

    /// <summary>When settlement occurred.</summary>
    public DateTimeOffset? SettlementTs { get; init; }

    /// <summary>Occurrence time for the underlying event.</summary>
    public DateTimeOffset? OccurrenceDatetime { get; init; }

    /// <summary>
    /// Settlement timer in seconds.
    /// </summary>
    public int? SettlementTimerSeconds { get; init; }

    /// <summary>
    /// The settlement result if market is settled.
    /// </summary>
    public string? Result { get; init; }

    /// <summary>
    /// The expiration value if settled.
    /// </summary>
    public string? ExpirationValue { get; init; }

    /// <summary>
    /// Whether this market can close early.
    /// </summary>
    public bool CanCloseEarly { get; init; }

    /// <summary>
    /// Category of this market.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Primary rules for the market.
    /// </summary>
    public string? RulesPrimary { get; init; }

    /// <summary>
    /// Secondary rules for the market.
    /// </summary>
    public string? RulesSecondary { get; init; }

    /// <summary>
    /// Yes subtitle for display.
    /// </summary>
    public string? YesSubTitle { get; init; }

    /// <summary>
    /// No subtitle for display.
    /// </summary>
    public string? NoSubTitle { get; init; }

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

    /// <summary>A supported price interval.</summary>
    public sealed record PriceRange
    {
        /// <summary>Inclusive start price in dollars.</summary>
        public required string Start { get; init; }

        /// <summary>Inclusive end price in dollars.</summary>
        public required string End { get; init; }

        /// <summary>Price step in dollars.</summary>
        public required string Step { get; init; }
    }
}
