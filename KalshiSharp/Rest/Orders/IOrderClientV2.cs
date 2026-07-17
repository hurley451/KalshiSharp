using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Orders
{
    /// <summary>
    /// Client for order management operations on the Kalshi exchange (V2 events endpoint).
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
    }
}
