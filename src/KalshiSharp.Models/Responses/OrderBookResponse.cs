namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents the order book for a market.
/// </summary>
public sealed record OrderBookResponse
{
    /// <summary>
    /// Market ticker this order book is for.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Bids on the Yes side, ordered by price descending.
    /// Each entry is [price, quantity].
    /// </summary>
    public required IReadOnlyList<int[]> Yes { get; init; }

    /// <summary>
    /// Bids on the No side, ordered by price descending.
    /// Each entry is [price, quantity].
    /// </summary>
    public required IReadOnlyList<int[]> No { get; init; }
}
