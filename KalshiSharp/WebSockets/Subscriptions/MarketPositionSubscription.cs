namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>
/// Subscription for real-time market position updates (private channel).
/// Receives <see cref="KalshiSharp.Models.WebSocket.MarketPositionUpdate"/> messages
/// when your positions are updated.
/// </summary>
/// <remarks>
/// This is a private channel that requires authentication.
/// Unlike public channels, the market_positions channel provides updates for
/// your own positions only.
/// </remarks>
public sealed record MarketPositionSubscription : WebSocketSubscription
{
    /// <summary>
    /// Channel identifier for market position updates.
    /// </summary>
    public const string ChannelName = "market_positions";

    /// <inheritdoc />
    public override string Channel => ChannelName;

    /// <summary>
    /// Creates an market position subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>An market position subscription.</returns>
    public static MarketPositionSubscription ForMarkets(params string[] marketTickers) =>
        new() { Markets = marketTickers };

    /// <summary>
    /// Creates an market position subscription for the specified market tickers.
    /// </summary>
    /// <param name="marketTickers">The market tickers to subscribe to.</param>
    /// <returns>An market position subscription.</returns>
    public static MarketPositionSubscription ForMarkets(IEnumerable<string> marketTickers) =>
        new() { Markets = marketTickers.ToList() };

    /// <summary>
    /// Creates an market position subscription for all markets (no market filter).
    /// </summary>
    /// <returns>An market position subscription for all markets.</returns>
    public static MarketPositionSubscription ForAllMarkets() => new();
}
