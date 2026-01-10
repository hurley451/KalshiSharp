namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>
/// Subscription for real-time order updates (private channel).
/// Receives <see cref="KalshiSharp.Models.WebSocket.OrderUpdate"/> messages
/// when your orders are created, filled, partially filled, or cancelled.
/// </summary>
/// <remarks>
/// This is a private channel that requires authentication.
/// Unlike public channels, the order channel provides updates for
/// your own orders only.
/// </remarks>
public sealed record OrderSubscription : WebSocketSubscription
{
    /// <summary>
    /// Channel identifier for order updates.
    /// </summary>
    public const string ChannelName = "order";

    /// <inheritdoc />
    public override string Channel => ChannelName;

    /// <summary>
    /// Creates an order subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>An order subscription.</returns>
    public static OrderSubscription ForMarkets(params string[] marketTickers) =>
        new() { Markets = marketTickers };

    /// <summary>
    /// Creates an order subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>An order subscription.</returns>
    public static OrderSubscription ForMarkets(IEnumerable<string> marketTickers) =>
        new() { Markets = marketTickers.ToList() };

    /// <summary>
    /// Creates an order subscription for all markets (no market filter).
    /// </summary>
    /// <returns>An order subscription for all markets.</returns>
    public static OrderSubscription ForAllMarkets() => new();
}
