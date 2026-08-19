namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response returned after successfully cancelling a V2 order.
/// </summary>
public sealed record CancelOrderResponseV2
{
    /// <summary>
    /// Unique identifier of the cancelled order.
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// Number of contracts that were canceled, i.e. the remaining count at time of cancellation (e.g. "10.00").
    /// </summary>
    public required string ReducedBy { get; init; }

    /// <summary>
    /// Matching engine timestamp at which the cancellation was processed, as Unix epoch milliseconds.
    /// </summary>
    public required long TsMs { get; init; }

    /// <summary>
    /// Client-provided order ID for correlation, if supplied on the original order.
    /// </summary>
    public string? ClientOrderId { get; init; }
}
