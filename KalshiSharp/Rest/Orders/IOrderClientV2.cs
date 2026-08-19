using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Orders;

/// <summary>
/// Client for active V2 event-order mutations.
/// </summary>
public interface IOrderClientV2
{
        /// <summary>
        /// Creates a new order.
        /// </summary>
        /// <param name="request">The order creation request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created order details.</returns>
        Task<CreateOrderResponseV2> CreateOrderAsync(CreateOrderRequestV2 request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an existing order.
        /// </summary>
        /// <param name="orderId">The order ID to cancel.</param>
        /// <param name="query">Optional query parameters (subaccount, exchange shard).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cancellation details.</returns>
        Task<CancelOrderResponseV2> CancelOrderAsync(string orderId, CancelOrderQueryV2? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Amends an existing order (price and/or quantity).
        /// </summary>
        /// <param name="orderId">The order ID to amend.</param>
        /// <param name="request">The amendment request.</param>
        /// <param name="subaccount">Optional subaccount number (0 for primary, 1-63 for subaccounts).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The amendment details.</returns>
        Task<AmendOrderResponseV2> AmendOrderAsync(string orderId, AmendOrderRequestV2 request, int? subaccount = null, CancellationToken cancellationToken = default);

        /// <summary>Decreases an existing order by or to a fixed-point quantity.</summary>
        Task<DecreaseOrderResponseV2> DecreaseOrderAsync(
            string orderId,
            DecreaseOrderRequestV2 request,
            int? subaccount = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("This V2 order client does not support decreasing orders.");

        /// <summary>Creates multiple V2 event orders in one request.</summary>
        Task<BatchCreateOrdersResponseV2> BatchCreateOrdersAsync(
            BatchCreateOrdersRequestV2 request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("This V2 order client does not support batch order creation.");

        /// <summary>Cancels multiple V2 event orders in one request.</summary>
        Task<BatchCancelOrdersResponseV2> BatchCancelOrdersAsync(
            BatchCancelOrdersRequestV2 request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("This V2 order client does not support batch order cancellation.");
}
