namespace KalshiSharp.Models.Responses;

/// <summary>
/// Wrapper response for a single market from the API.
/// </summary>
public sealed record SingleEventResponse
{
    /// <summary>
    /// The event
    /// </summary>
    public required EventResponse Event { get; init; }
}

/// <summary>
/// Wrapper response for a single market from the API.
/// </summary>
public sealed record SingleOrderResponse
{
    /// <summary>
    /// The order
    /// </summary>
    public required OrderResponse Order { get; init; }
}