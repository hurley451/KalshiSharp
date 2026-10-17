namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>Subscription for authenticated RFQ and quote notifications.</summary>
public sealed record CommunicationsSubscription : WebSocketSubscription
{
    /// <summary>Communications channel identifier.</summary>
    public const string ChannelName = "communications";

    /// <inheritdoc />
    public override string Channel => ChannelName;

    /// <summary>When true, receive RFQ events only for RFQs created by the authenticated user.</summary>
    public bool SelfOnly { get; init; }

    /// <summary>Optional fanout shard count, from 1 through 100.</summary>
    public int? ShardFactor { get; init; }

    /// <summary>Optional shard key, which must be less than <see cref="ShardFactor"/>.</summary>
    public int? ShardKey { get; init; }

    /// <inheritdoc />
    internal override SubscriptionParams CreateSubscribeParams()
    {
        if ((ShardFactor is null) != (ShardKey is null))
        {
            throw new ArgumentException("ShardFactor and ShardKey must be supplied together.");
        }

        if (ShardFactor is { } factor && (factor is < 1 or > 100 || ShardKey is < 0 || ShardKey >= factor))
        {
            throw new ArgumentOutOfRangeException(nameof(ShardKey), "ShardKey must be non-negative and less than ShardFactor, which must be from 1 through 100.");
        }

        return new SubscriptionParams
        {
            Channels = [ChannelName],
            UserFilter = SelfOnly ? "self" : null,
            ShardFactor = ShardFactor,
            ShardKey = ShardKey
        };
    }
}
