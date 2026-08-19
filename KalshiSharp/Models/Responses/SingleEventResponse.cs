namespace KalshiSharp.Models.Responses;

/// <summary>
/// Wrapper returned by the Get Event endpoint.
/// </summary>
public sealed record SingleEventResponse
{
    /// <summary>Event details.</summary>
    public required EventResponse Event { get; init; }

    /// <summary>
    /// Markets returned outside the event when nested markets are not requested.
    /// </summary>
    public IReadOnlyList<MarketResponse>? Markets { get; init; }
}
