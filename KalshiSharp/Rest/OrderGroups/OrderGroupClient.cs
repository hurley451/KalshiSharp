using KalshiSharp.Http;
using KalshiSharp.Models.Common;
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

    public Task<GetOrderGroupResponse> GetOrderGroupAsync(
        string orderGroupId,
        int? subaccount = null,
        CancellationToken cancellationToken = default)
    {
        SettlementQuery.ValidateSubaccount(subaccount);

        var builder = new QueryStringBuilder();
        builder.AppendIfNotNull("subaccount", subaccount);

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{EscapeOrderGroupId(orderGroupId)}{builder.Build()}"
        };
        return httpClient.SendAsync<GetOrderGroupResponse>(request, cancellationToken);
    }

    public Task<CreateOrderGroupResponse> CreateOrderGroupAsync(
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
        return httpClient.SendAsync<CreateOrderGroupResponse>(httpRequest, cancellationToken);
    }

    public Task UpdateOrderGroupAsync(
        string orderGroupId,
        UpdateOrderGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateOrderGroupWrite(request);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Put,
            Path = $"{BasePath}/{EscapeOrderGroupId(orderGroupId)}/limit{BuildActionQuery(request)}",
            Content = new UpdateOrderGroupLimitRequest
            {
                ContractsLimit = request.ContractsLimit,
                ContractsLimitFp = request.ContractsLimitFp
            }
        };
        return httpClient.SendAsync(httpRequest, cancellationToken);
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
            Path = $"{path}{BuildActionQuery(content)}"
        };
        return httpClient.SendAsync(request, cancellationToken);
    }

    private static string BuildActionQuery(object request)
    {
        var builder = new QueryStringBuilder();
        if (request is OrderGroupActionRequest actionRequest)
        {
            builder.AppendIfNotNull("subaccount", actionRequest.Subaccount);
            builder.AppendIfNotNull("exchange_index", actionRequest.ExchangeIndex);
        }

        return builder.Build();
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

    private sealed record UpdateOrderGroupLimitRequest
    {
        public long? ContractsLimit { get; init; }

        public string? ContractsLimitFp { get; init; }
    }
}
