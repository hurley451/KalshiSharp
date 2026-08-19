namespace KalshiSharp.Models.Responses;

/// <summary>Response returned after cancelling a batch of V2 event orders.</summary>
public sealed record BatchCancelOrdersResponseV2
{
    /// <summary>Cancellation results for the submitted orders.</summary>
    public required IReadOnlyList<CancelOrderResponseV2> Orders { get; init; }
}
