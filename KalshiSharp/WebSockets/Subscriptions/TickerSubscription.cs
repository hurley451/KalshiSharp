namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>
/// Subscription for real-time market price, volume, and open interest updates.
/// Receives <see cref="KalshiSharp.Models.WebSocket.TickerUpdate"/> when updates occur.
/// </summary>
public sealed record TickerSubscription : WebSocketSubscription
{
    /// <summary>
    /// Channel identifier for ticker updates.
    /// </summary>
    public const string ChannelName = "ticker";

    /// <inheritdoc />
    public override string Channel => ChannelName;

    /// <summary>
    /// Creates a ticker subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>A ticker subscription.</returns>
    public static TickerSubscription ForMarkets(params string[] marketTickers) =>
        new() { Markets = marketTickers };

    /// <summary>
    /// Creates a ticker subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>A ticker subscription.</returns>
    public static TickerSubscription ForMarkets(IEnumerable<string> marketTickers) =>
        new() { Markets = marketTickers.ToList() };
}
