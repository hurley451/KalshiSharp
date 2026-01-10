namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents the current status of the Kalshi exchange.
/// </summary>
public sealed record ExchangeStatusResponse
{
    /// <summary>
    /// Whether the exchange is currently active and accepting orders.
    /// </summary>
    public required bool ExchangeActive { get; init; }

    /// <summary>
    /// Whether trading is currently enabled.
    /// </summary>
    public required bool TradingActive { get; init; }
}
