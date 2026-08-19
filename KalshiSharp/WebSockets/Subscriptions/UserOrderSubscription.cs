namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>Subscription for current private user-order updates.</summary>
public sealed record UserOrderSubscription : WebSocketSubscription
{
    /// <summary>Current channel identifier.</summary>
    public const string ChannelName = "user_orders";

    /// <inheritdoc />
    public override string Channel => ChannelName;

    /// <summary>Creates a subscription for specified markets.</summary>
    public static UserOrderSubscription ForMarkets(params string[] marketTickers) =>
        new() { Markets = marketTickers };

    /// <summary>Creates a subscription for all markets.</summary>
    public static UserOrderSubscription ForAllMarkets() => new();
}
