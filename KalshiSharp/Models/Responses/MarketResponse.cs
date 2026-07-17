using KalshiSharp.Models.Enums;
using System.Text.Json.Serialization;

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
    /// Price for the highest YES buy offer on this market in dollars.
    /// </summary>
    [JsonPropertyName("yes_bid_dollars")]
    public string? YesBidDollars { get; init; }

    /// <summary>
    /// Total contract size of orders to buy YES at the best bid price (fixed-point count string).
    /// </summary>
    [JsonPropertyName("yes_bid_size_fp")]
    public string? YesBidSizeFp { get; init; }

    /// <summary>
    /// Price for the lowest YES sell offer on this market in dollars.
    /// </summary>
    [JsonPropertyName("yes_ask_dollars")]
    public string? YesAskDollars { get; init; }

    /// <summary>
    /// Total contract size of orders to sell YES at the best ask price (fixed-point count string).
    /// </summary>
    [JsonPropertyName("yes_ask_size_fp")]
    public string? YesAskSizeFp { get; init; }

    /// <summary>
    /// Price for the highest NO buy offer on this market in dollars.
    /// </summary>
    [JsonPropertyName("no_bid_dollars")]
    public string? NoBidDollars { get; init; }

    /// <summary>
    /// Price for the lowest NO sell offer on this market in dollars.
    /// </summary>
    [JsonPropertyName("no_ask_dollars")]
    public string? NoAskDollars { get; init; }

    /// <summary>
    /// Price for the last traded YES contract on this market in dollars.
    /// </summary>
    [JsonPropertyName("last_price_dollars")]
    public string? LastPriceDollars { get; init; }

    /// <summary>
    /// String representation of the market volume in contracts.
    /// </summary>
    [JsonPropertyName("volume_fp")]
    public string? VolumeFp { get; init; }

    /// <summary>
    /// String representation of the 24h market volume in contracts.
    /// </summary>
    [JsonPropertyName("volume_24h_fp")]
    public string? Volume24hFp { get; init; }

    /// <summary>
    /// String representation of the number of contracts bought on this market disconsidering netting.
    /// </summary>
    [JsonPropertyName("open_interest_fp")]
    public string? OpenInterestFp { get; init; }

    /// <summary>
    /// The total value of a single contract at settlement in dollars.
    /// </summary>
    [JsonPropertyName("notional_value_dollars")]
    public string? NotionalValueDollars { get; init; }

    /// <summary>
    /// Price for the highest YES buy offer on this market a day ago in dollars.
    /// </summary>
    [JsonPropertyName("previous_yes_bid_dollars")]
    public string? PreviousYesBidDollars { get; init; }

    /// <summary>
    /// Price for the lowest YES sell offer on this market a day ago in dollars.
    /// </summary>
    [JsonPropertyName("previous_yes_ask_dollars")]
    public string? PreviousYesAskDollars { get; init; }

    /// <summary>
    /// Price for the last traded YES contract on this market a day ago in dollars.
    /// </summary>
    [JsonPropertyName("previous_price_dollars")]
    public string? PreviousPriceDollars { get; init; }

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
}
