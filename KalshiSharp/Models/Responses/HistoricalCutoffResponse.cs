namespace KalshiSharp.Models.Responses;

/// <summary>Cutoff timestamps separating live and archived exchange data.</summary>
public sealed record HistoricalCutoffResponse
{
    /// <summary>Market settlement cutoff.</summary>
    public required DateTimeOffset MarketSettledTs { get; init; }

    /// <summary>Trade creation cutoff.</summary>
    public required DateTimeOffset TradesCreatedTs { get; init; }

    /// <summary>Order update cutoff.</summary>
    public required DateTimeOffset OrdersUpdatedTs { get; init; }

    /// <summary>Market-position update cutoff.</summary>
    public DateTimeOffset? MarketPositionsLastUpdatedTs { get; init; }
}
