namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>Subscription for standard market and event lifecycle updates.</summary>
public sealed record MarketLifecycleSubscription : WebSocketSubscription
{
    /// <summary>Lifecycle channel identifier.</summary>
    public const string ChannelName = "market_lifecycle_v2";

    /// <inheritdoc />
    public override string Channel => ChannelName;

    /// <inheritdoc />
    internal override SubscriptionParams CreateSubscribeParams() => new()
    {
        Channels = [ChannelName]
    };
}
