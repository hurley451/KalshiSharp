namespace KalshiSharp.Models.Responses;

public sealed record SingleEventResponse
{
    public required EventResponse Event { get; init; }
}

public sealed record SingleOrderResponse
{
    public required OrderResponse Order { get; init; }
}