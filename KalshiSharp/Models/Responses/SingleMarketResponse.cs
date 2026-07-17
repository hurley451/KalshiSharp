namespace KalshiSharp.Models.Responses;

public sealed record SingleMarketResponse
{
    public required MarketResponse Market { get; init; }
}
