namespace KalshiSharp.Models.Requests;

/// <summary>Request to cancel multiple V2 event orders.</summary>
public sealed record BatchCancelOrdersRequestV2
{
    /// <summary>Orders to cancel.</summary>
    public required IReadOnlyList<BatchCancelOrderItemV2> Orders { get; init; }
}

/// <summary>Identifies one V2 event order in a batch cancellation.</summary>
public sealed record BatchCancelOrderItemV2
{
    /// <summary>Order identifier.</summary>
    public required string OrderId { get; init; }

    /// <summary>Subaccount that owns the order.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Exchange shard that owns the order.</summary>
    public int? ExchangeIndex { get; init; }
}
