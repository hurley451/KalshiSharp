namespace KalshiSharp.Models.Enums;

/// <summary>
/// Represents the side of the order book for a V2 order.
/// </summary>
/// <remarks>
/// Serialized as lowercase strings: "bid", "ask".
/// For event markets, this refers to the YES leg only: bid means buy YES, ask means sell YES.
/// Selling YES is economically equivalent to buying NO at 1 - price.
/// </remarks>
public enum OrderBookSide
{
    /// <summary>
    /// Bid — buy YES contracts.
    /// </summary>
    Bid,

    /// <summary>
    /// Ask — sell YES contracts (equivalent to buying NO at 1 - price).
    /// </summary>
    Ask
}
