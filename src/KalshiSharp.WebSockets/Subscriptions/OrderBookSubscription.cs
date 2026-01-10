namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>
/// Subscription for real-time order book updates.
/// Receives <see cref="KalshiSharp.Models.WebSocket.OrderBookSnapshotMessage"/> on initial subscription
/// and <see cref="KalshiSharp.Models.WebSocket.OrderBookUpdate"/> for subsequent changes.
/// </summary>
public sealed record OrderBookSubscription : WebSocketSubscription
{
    /// <summary>
    /// Channel identifier for order book delta updates.
    /// </summary>
    public const string ChannelName = "orderbook_delta";

    /// <inheritdoc />
    public override string Channel => ChannelName;

    /// <summary>
    /// Creates an order book subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>An order book subscription.</returns>
    public static OrderBookSubscription ForMarkets(params string[] marketTickers) =>
        new() { Markets = marketTickers };

    /// <summary>
    /// Creates an order book subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>An order book subscription.</returns>
    public static OrderBookSubscription ForMarkets(IEnumerable<string> marketTickers) =>
        new() { Markets = marketTickers.ToList() };
}
