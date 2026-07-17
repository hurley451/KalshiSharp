using KalshiSharp.Http;
using KalshiSharp.Models.Common;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Events;

/// <summary>
/// Implementation of the event client for event-related endpoints.
/// </summary>
internal sealed class EventClient : IEventClient
{
    private const string BasePath = "/trade-api/v2/events";

    private readonly IKalshiHttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public EventClient(IKalshiHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<EventResponse> GetEventAsync(string eventTicker, bool? withNestedMarkets = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventTicker);

        var builder = new QueryStringBuilder();
        if (withNestedMarkets.HasValue)
        {
            builder.Append("with_nested_markets", withNestedMarkets.Value ? "true" : "false");
        }

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}/{Uri.EscapeDataString(eventTicker)}{builder.Build()}"
        };

        var response = await _httpClient.SendAsync<SingleEventResponse>(request, cancellationToken);
        return response.Event;
    }

    /// <inheritdoc />
    public Task<EventsResponse> ListEventsAsync(EventQuery? query = null, CancellationToken cancellationToken = default)
    {
        var queryString = query?.ToQueryString() ?? string.Empty;

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = $"{BasePath}{queryString}"
        };

        return _httpClient.SendAsync<EventsResponse>(request, cancellationToken);
    }
}
