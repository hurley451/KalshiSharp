namespace KalshiSharp.Models.Requests;

/// <summary>
/// Request to decrease the remaining quantity of a V2 event order.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="ReduceBy"/> or <see cref="ReduceTo"/> must be provided.
/// </remarks>
public sealed record DecreaseOrderRequestV2
{
    /// <summary>Number of contracts to remove from the resting quantity.</summary>
    public string? ReduceBy { get; init; }

    /// <summary>Target resting quantity after the decrease.</summary>
    public string? ReduceTo { get; init; }

    /// <summary>Identifier for an exchange shard. Defaults to 0 if unspecified.</summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>Market ticker used to auto-route when <see cref="ExchangeIndex"/> is -1.</summary>
    public string? MarketTicker { get; init; }
}
