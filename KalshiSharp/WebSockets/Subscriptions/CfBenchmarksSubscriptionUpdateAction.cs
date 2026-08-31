namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>Action applied to an existing CF Benchmarks value subscription.</summary>
public enum CfBenchmarksSubscriptionUpdateAction
{
    /// <summary>Adds index identifiers to the subscription.</summary>
    SubscribeIndices,

    /// <summary>Removes index identifiers from the subscription.</summary>
    UnsubscribeIndices,

    /// <summary>Requests the available index identifiers without changing the subscription.</summary>
    IndexList
}
