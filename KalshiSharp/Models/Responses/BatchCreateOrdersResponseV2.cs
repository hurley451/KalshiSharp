namespace KalshiSharp.Models.Responses;

/// <summary>Response returned after creating a batch of V2 event orders.</summary>
public sealed record BatchCreateOrdersResponseV2
{
    /// <summary>Results for the submitted orders.</summary>
    public required IReadOnlyList<CreateOrderResponseV2> Orders { get; init; }
}
