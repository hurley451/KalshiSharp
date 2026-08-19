namespace KalshiSharp.Models.Requests;

/// <summary>Request to create multiple V2 event orders.</summary>
public sealed record BatchCreateOrdersRequestV2
{
    /// <summary>Orders to create.</summary>
    public required IReadOnlyList<CreateOrderRequestV2> Orders { get; init; }
}
