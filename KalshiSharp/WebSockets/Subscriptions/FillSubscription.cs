namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>
/// Subscription for real-time fill updates (private channel).
/// Receives <see cref="KalshiSharp.Models.WebSocket.FillUpdate"/> messages
/// when your fills are updated.
/// </summary>
/// <remarks>
/// This is a private channel that requires authentication.
/// Unlike public channels, the fill channel provides updates for
/// your own fills only.
/// </remarks>
public sealed record FillSubscription : WebSocketSubscription
{
    /// <summary>
    /// Channel identifier for market position updates.
    /// </summary>
    public const string ChannelName = "fill";

    /// <inheritdoc />
    public override string Channel => ChannelName;

    /// <summary>
    /// Creates an fill subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>An market position subscription.</returns>
    public static FillSubscription ForMarkets(params string[] marketTickers) =>
        new() { Markets = marketTickers };

    /// <summary>
    /// Creates an fill subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>An market position subscription.</returns>
    public static FillSubscription ForMarkets(IEnumerable<string> marketTickers) =>
        new() { Markets = marketTickers.ToList() };

    /// <summary>
    /// Creates an fill subscription for all markets (no market filter).
    /// </summary>
    /// <returns>An fill subscription for all markets.</returns>
    public static FillSubscription ForAllMarkets() => new();
}
