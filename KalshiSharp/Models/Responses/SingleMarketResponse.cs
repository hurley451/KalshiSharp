namespace KalshiSharp.Models.Responses;

/// <summary>
/// Wrapper response for a single market from the API.
/// </summary>
public sealed record SingleMarketResponse
{
    /// <summary>
    /// The market data.
    /// </summary>
    public required MarketResponse Market { get; init; }
}
