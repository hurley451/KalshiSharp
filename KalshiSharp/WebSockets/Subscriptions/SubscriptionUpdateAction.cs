namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>Action applied to an existing WebSocket subscription.</summary>
public enum SubscriptionUpdateAction
{
    /// <summary>Add markets to the subscription.</summary>
    AddMarkets,

    /// <summary>Remove markets from the subscription.</summary>
    DeleteMarkets,

    /// <summary>Request a fresh order-book snapshot.</summary>
    GetSnapshot
}
