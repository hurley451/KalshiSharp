namespace KalshiSharp.Models.Responses;

/// <summary>Wrapper returned by the Get Order endpoint.</summary>
public sealed record SingleOrderResponse
{
    /// <summary>Order details.</summary>
    public required OrderResponse Order { get; init; }
}
