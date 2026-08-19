namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>Subscription for multivariate market and event lifecycle updates.</summary>
public sealed record MultivariateMarketLifecycleSubscription : WebSocketSubscription
{
    /// <summary>Lifecycle channel identifier.</summary>
    public const string ChannelName = "multivariate_market_lifecycle";

    /// <inheritdoc />
    public override string Channel => ChannelName;

    /// <inheritdoc />
    internal override SubscriptionParams CreateSubscribeParams() => new()
    {
        Channels = [ChannelName]
    };
}
