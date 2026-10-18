using KalshiSharp.Models.Responses;
using KalshiSharp.Models.Requests;

namespace KalshiSharp.Rest.Exchange;

/// <summary>
/// Client for Kalshi exchange status and schedule endpoints.
/// </summary>
public interface IExchangeClient
{
    /// <summary>Gets public series fee changes, optionally including history. No pagination is defined.</summary>
    Task<SeriesFeeChangesResponse> GetSeriesFeeChangesAsync(SeriesFeeChangesQuery? query = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This exchange client does not support series fee changes.");

    /// <summary>
    /// Gets the current status of the Kalshi exchange.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exchange status.</returns>
    Task<ExchangeStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the trading schedule of the Kalshi exchange.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exchange schedule.</returns>
    Task<ExchangeScheduleResponse> GetScheduleAsync(CancellationToken cancellationToken = default);
}
