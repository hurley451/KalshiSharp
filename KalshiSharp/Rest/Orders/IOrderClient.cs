using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Orders;

/// <summary>
/// Client for order management operations on the Kalshi exchange.
/// </summary>
public interface IOrderClient
{
    /// <summary>
    /// Creates a new order on the exchange.
    /// </summary>
    /// <param name="request">The order creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created order details.</returns>
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Amends an existing order (price and/or quantity).
    /// </summary>
    /// <param name="orderId">The order ID to amend.</param>
    /// <param name="request">The amendment request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated order details.</returns>
    Task<OrderResponse> AmendOrderAsync(string orderId, AmendOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an existing order.
    /// </summary>
    /// <param name="orderId">The order ID to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cancelled order details.</returns>
    Task<OrderResponse> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific order by ID.
    /// </summary>
    /// <param name="orderId">The order ID to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order details.</returns>
    Task<OrderResponse> GetOrderAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists orders with optional filtering and pagination.
    /// </summary>
    /// <param name="query">Optional query parameters for filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of orders.</returns>
    Task<OrdersResponse> ListOrdersAsync(OrderQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Lists queue positions for resting orders.</summary>
    Task<QueuePositionsResponse> ListQueuePositionsAsync(QueuePositionsQuery? query = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This order client does not support queue positions.");

    /// <summary>Gets the queue position for one order.</summary>
    Task<QueuePositionResponse> GetQueuePositionAsync(string orderId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This order client does not support queue positions.");
}
