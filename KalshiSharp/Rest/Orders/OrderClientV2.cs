using KalshiSharp.Http;
using KalshiSharp.Models.Common;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Orders;

/// <summary>
/// Implementation of the V2 order client for the events orders endpoint.
/// </summary>
internal sealed class OrderClientV2 : IOrderClientV2
{
    private const string BasePath = "/trade-api/v2/portfolio/events/orders";

    private readonly IKalshiHttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderClientV2"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public OrderClientV2(IKalshiHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public Task<CreateOrderResponseV2> CreateOrderAsync(CreateOrderRequestV2 request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = BasePath,
            Content = request
        };

        return _httpClient.SendAsync<CreateOrderResponseV2>(httpRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CancelOrderResponseV2> CancelOrderAsync(string orderId, CancelOrderQueryV2? query = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);

        var queryString = query?.ToQueryString() ?? string.Empty;

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Delete,
            Path = $"{BasePath}/{Uri.EscapeDataString(orderId)}{queryString}"
        };

        return _httpClient.SendAsync<CancelOrderResponseV2>(httpRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AmendOrderResponseV2> AmendOrderAsync(string orderId, AmendOrderRequestV2 request, int? subaccount = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        ArgumentNullException.ThrowIfNull(request);

        var queryString = string.Empty;
        if (subaccount.HasValue)
        {
            var builder = new QueryStringBuilder();
            builder.AppendIfNotNull("subaccount", subaccount);
            queryString = builder.Build();
        }

        var httpRequest = new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = $"{BasePath}/{Uri.EscapeDataString(orderId)}/amend{queryString}",
            Content = request
        };

        return _httpClient.SendAsync<AmendOrderResponseV2>(httpRequest, cancellationToken);
    }
}
