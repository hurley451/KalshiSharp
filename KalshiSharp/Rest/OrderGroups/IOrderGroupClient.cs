using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.OrderGroups;

/// <summary>Client for Kalshi order-group operations.</summary>
public interface IOrderGroupClient
{
    /// <summary>Lists order groups with optional pagination and subaccount filtering.</summary>
    Task<OrderGroupsResponse> ListOrderGroupsAsync(
        OrderGroupQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a single order group by identifier.</summary>
    Task<GetOrderGroupResponse> GetOrderGroupAsync(
        string orderGroupId,
        int? subaccount = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates an order group.</summary>
    Task<CreateOrderGroupResponse> CreateOrderGroupAsync(
        CreateOrderGroupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Updates an order group.</summary>
    Task UpdateOrderGroupAsync(
        string orderGroupId,
        UpdateOrderGroupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Triggers an order group.</summary>
    Task TriggerOrderGroupAsync(
        string orderGroupId,
        TriggerOrderGroupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Resets an order group.</summary>
    Task ResetOrderGroupAsync(
        string orderGroupId,
        ResetOrderGroupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes an order group.</summary>
    Task DeleteOrderGroupAsync(
        string orderGroupId,
        DeleteOrderGroupRequest request,
        CancellationToken cancellationToken = default);
}
