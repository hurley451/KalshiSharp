namespace KalshiSharp.Models.WebSocket;

/// <summary>Order-group update pushed over the private WebSocket stream.</summary>
public sealed record OrderGroupUpdate : WebSocketMessage<OrderGroupMessage>
{
    /// <inheritdoc />
    public override string Type => "order_group_update";
}

/// <summary>Order-group WebSocket payload.</summary>
public sealed record OrderGroupMessage
{
    /// <summary>Order-group identifier.</summary>
    public required string OrderGroupId { get; init; }

    /// <summary>Current order-group status.</summary>
    public string? Status { get; init; }

    /// <summary>Whether the group has reached its matching limit and cancelled associated orders.</summary>
    public bool? Triggered { get; init; }

    /// <summary>Exchange shard for the order group.</summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>Subaccount for the order group.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Maximum number of contracts that can match in the rolling order-group window.</summary>
    public long? ContractsLimit { get; init; }

    /// <summary>Maximum fixed-point number of contracts that can match in the rolling order-group window.</summary>
    public string? ContractsLimitFp { get; init; }

    /// <summary>Order identifiers currently associated with the group.</summary>
    public IReadOnlyList<string>? OrderIds { get; init; }
}
