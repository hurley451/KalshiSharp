using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Query parameters for listing order groups.</summary>
public sealed record OrderGroupQuery : PaginationParameters
{
    /// <summary>Filter by subaccount, including zero for the primary account.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        SettlementQuery.ValidateLimit(Limit);
        SettlementQuery.ValidateSubaccount(Subaccount);

        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.AppendIfNotNull("subaccount", Subaccount);
        return builder.Build();
    }
}

/// <summary>Base request carrying shard and subaccount context for order-group operations.</summary>
public abstract record OrderGroupActionRequest
{
    /// <summary>Exchange shard for the order group.</summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>Subaccount for the order group, including zero for the primary account.</summary>
    public int? Subaccount { get; init; }
}

/// <summary>Base request for creating or updating an order group.</summary>
public abstract record OrderGroupWriteRequest : OrderGroupActionRequest
{
    /// <summary>Maximum number of contracts that can match in the rolling order-group window.</summary>
    public long? ContractsLimit { get; init; }

    /// <summary>Maximum fixed-point number of contracts that can match in the rolling order-group window.</summary>
    public string? ContractsLimitFp { get; init; }
}

/// <summary>Request for creating an order group.</summary>
public sealed record CreateOrderGroupRequest : OrderGroupWriteRequest;

/// <summary>Request for updating an order group.</summary>
public sealed record UpdateOrderGroupRequest : OrderGroupWriteRequest;

/// <summary>Request for triggering an order group.</summary>
public sealed record TriggerOrderGroupRequest : OrderGroupActionRequest;

/// <summary>Request for resetting an order group.</summary>
public sealed record ResetOrderGroupRequest : OrderGroupActionRequest;

/// <summary>Request for deleting an order group.</summary>
public sealed record DeleteOrderGroupRequest : OrderGroupActionRequest;
