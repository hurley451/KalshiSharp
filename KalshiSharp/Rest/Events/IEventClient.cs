using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Events;

/// <summary>
/// Client for Kalshi event endpoints.
/// </summary>
public interface IEventClient
{
    /// <summary>Gets one public page of scheduled event fee overrides, including nullable override clears.</summary>
    Task<EventFeeChangesResponse> GetEventFeeChangesAsync(EventFeeChangesQuery? query = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This event client does not support event fee changes.");

    /// <summary>
    /// Gets a single event by ticker.
    /// </summary>
    /// <param name="eventTicker">The event ticker.</param>
    /// <param name="withNestedMarkets">Whether to include nested markets in the response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event details.</returns>
    Task<EventResponse> GetEventAsync(string eventTicker, bool? withNestedMarkets = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets event metadata without loading child markets.
    /// </summary>
    /// <param name="eventTicker">The event ticker.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event metadata.</returns>
    Task<EventResponse> GetEventMetadataAsync(string eventTicker, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This event client does not support metadata-only event reads.");

    /// <summary>
    /// Lists events with optional filtering and pagination.
    /// </summary>
    /// <param name="query">Optional query parameters for filtering and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of events.</returns>
    Task<EventsResponse> ListEventsAsync(EventQuery? query = null, CancellationToken cancellationToken = default);
}
