namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>
/// Subscription for real-time trade updates.
/// Receives <see cref="KalshiSharp.Models.WebSocket.TradeUpdate"/> messages when trades occur.
/// </summary>
public sealed record TradeSubscription : WebSocketSubscription
{
    /// <summary>
    /// Channel identifier for trade updates.
    /// </summary>
    public const string ChannelName = "trade";

    /// <inheritdoc />
    public override string Channel => ChannelName;

    /// <summary>
    /// Creates a trade subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>A trade subscription.</returns>
    public static TradeSubscription ForMarkets(params string[] marketTickers) =>
        new() { Markets = marketTickers };

    /// <summary>
    /// Creates a trade subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>A trade subscription.</returns>
    public static TradeSubscription ForMarkets(IEnumerable<string> marketTickers) =>
        new() { Markets = marketTickers.ToList() };
}
