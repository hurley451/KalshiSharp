using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Responses;

/// <summary>Response for listing order groups.</summary>
public sealed record OrderGroupsResponse : PagedResponse<OrderGroupResponse>
{
    /// <summary>Order groups in the current page.</summary>
    public IReadOnlyList<OrderGroupResponse> OrderGroups { get; init; } = [];

    /// <inheritdoc />
    public override IReadOnlyList<OrderGroupResponse> Items => OrderGroups;
}

/// <summary>Response wrapper for a single order group.</summary>
public sealed record SingleOrderGroupResponse
{
    /// <summary>The requested order group.</summary>
    public OrderGroupResponse? OrderGroup { get; init; }

    /// <summary>Order-group identifier returned by create operations.</summary>
    public string? OrderGroupId { get; init; }

    /// <summary>Subaccount for the order group returned by create operations.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Exchange shard for the order group returned by create operations.</summary>
    public int? ExchangeIndex { get; init; }
}

/// <summary>Order-group state and limits.</summary>
public sealed record OrderGroupResponse
{
    /// <summary>Order-group identifier.</summary>
    public string? OrderGroupId { get; init; }

    /// <summary>Alternate identifier shape used by summary payloads.</summary>
    public string? Id { get; init; }

    /// <summary>Current order-group status.</summary>
    public string? Status { get; init; }

    /// <summary>Whether the group has reached its matching limit and cancelled associated orders.</summary>
    public bool? Triggered { get; init; }

    /// <summary>Whether automatic cancellation is enabled for the group.</summary>
    public bool? IsAutoCancelEnabled { get; init; }

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

    /// <summary>Creation timestamp in Unix milliseconds.</summary>
    public long? CreatedTsMs { get; init; }

    /// <summary>Last update timestamp in Unix milliseconds.</summary>
    public long? UpdatedTsMs { get; init; }
}
