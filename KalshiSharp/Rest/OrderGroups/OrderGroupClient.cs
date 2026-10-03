using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.OrderGroups;

internal sealed class OrderGroupClient(IKalshiHttpClient httpClient) : IOrderGroupClient
{
    private const string BasePath = "/trade-api/v2/portfolio/order_groups";

    public Task<OrderGroupsResponse> ListOrderGroupsAsync(
        OrderGroupQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}{query?.ToQueryString()}"
        };
        return httpClient.SendAsync<OrderGroupsResponse>(request, cancellationToken);
    }

    public Task<SingleOrderGroupResponse> GetOrderGroupAsync(
        string orderGroupId,
        CancellationToken cancellationToken = default)
    {
        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{EscapeOrderGroupId(orderGroupId)}"
        };
        return httpClient.SendAsync<SingleOrderGroupResponse>(request, cancellationToken);
    }

    public Task<SingleOrderGroupResponse> CreateOrderGroupAsync(
        CreateOrderGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateOrderGroupWrite(request);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = $"{BasePath}/create",
            Content = request
        };
        return httpClient.SendAsync<SingleOrderGroupResponse>(httpRequest, cancellationToken);
    }

    public Task<SingleOrderGroupResponse> UpdateOrderGroupAsync(
        string orderGroupId,
        UpdateOrderGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateOrderGroupWrite(request);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Put,
            Path = $"{BasePath}/{EscapeOrderGroupId(orderGroupId)}/limit",
            Content = request
        };
        return httpClient.SendAsync<SingleOrderGroupResponse>(httpRequest, cancellationToken);
    }

    public Task TriggerOrderGroupAsync(
        string orderGroupId,
        TriggerOrderGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateActionRequest(request);
        return SendActionAsync(HttpMethod.Put, orderGroupId, "trigger", request, cancellationToken);
    }

    public Task ResetOrderGroupAsync(
        string orderGroupId,
        ResetOrderGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateActionRequest(request);
        return SendActionAsync(HttpMethod.Put, orderGroupId, "reset", request, cancellationToken);
    }

    public Task DeleteOrderGroupAsync(
        string orderGroupId,
        DeleteOrderGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateActionRequest(request);
        return SendActionAsync(HttpMethod.Delete, orderGroupId, null, request, cancellationToken);
    }

    private Task SendActionAsync(
        HttpMethod method,
        string orderGroupId,
        string? action,
        object content,
        CancellationToken cancellationToken)
    {
        var path = $"{BasePath}/{EscapeOrderGroupId(orderGroupId)}";
        if (!string.IsNullOrEmpty(action))
        {
            path += $"/{action}";
        }

        var request = new KalshiRequest
        {
            Method = method,
            Path = path,
            Content = content
        };
        return httpClient.SendAsync(request, cancellationToken);
    }

    private static string EscapeOrderGroupId(string orderGroupId)
    {
        if (string.IsNullOrWhiteSpace(orderGroupId))
        {
            throw new ArgumentException("Order group ID must not be empty.", nameof(orderGroupId));
        }

        return Uri.EscapeDataString(orderGroupId);
    }

    private static void ValidateOrderGroupWrite(OrderGroupWriteRequest request)
    {
        ValidateActionRequest(request);

        if (request.ContractsLimit is null && string.IsNullOrWhiteSpace(request.ContractsLimitFp))
        {
            throw new ArgumentException("Order group contract limit is required.", nameof(request));
        }

        if (request.ContractsLimit is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Contracts limit must be greater than zero.");
        }
    }

    private static void ValidateActionRequest(OrderGroupActionRequest request)
    {
        SettlementQuery.ValidateSubaccount(request.Subaccount);

        if (request.ExchangeIndex is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Exchange index must be nonnegative.");
        }
    }
}
