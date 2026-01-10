using KalshiSharp.Core.Http;
using KalshiSharp.Models.Common;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Orders;

/// <summary>
/// Implementation of the order client for order management endpoints.
/// </summary>
internal sealed class OrderClient : IOrderClient
{
    private const string BasePath = "/trade-api/v2/portfolio/orders";

    private readonly IKalshiHttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public OrderClient(IKalshiHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = BasePath,
            Content = request
        };

        return _httpClient.SendAsync<OrderResponse>(httpRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OrderResponse> AmendOrderAsync(string orderId, AmendOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        ArgumentNullException.ThrowIfNull(request);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Put,
            Path = $"{BasePath}/{Uri.EscapeDataString(orderId)}",
            Content = request
        };

        return _httpClient.SendAsync<OrderResponse>(httpRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OrderResponse> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Delete,
            Path = $"{BasePath}/{Uri.EscapeDataString(orderId)}"
        };

        return _httpClient.SendAsync<OrderResponse>(httpRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OrderResponse> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{Uri.EscapeDataString(orderId)}"
        };

        return _httpClient.SendAsync<OrderResponse>(httpRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedResponse<OrderResponse>> ListOrdersAsync(OrderQuery? query = null, CancellationToken cancellationToken = default)
    {
        var queryString = query?.ToQueryString() ?? string.Empty;

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}{queryString}"
        };

        return _httpClient.SendAsync<PagedResponse<OrderResponse>>(httpRequest, cancellationToken);
    }
}
