namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response returned after successfully creating a V2 order.
/// </summary>
public sealed record CreateOrderResponseV2
{
    /// <summary>
    /// Unique identifier for the created order.
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// Number of contracts filled immediately upon placement (e.g. "10.00").
    /// </summary>
    public required string FillCount { get; init; }

    /// <summary>
    /// Number of contracts remaining after placement.
    /// For IOC orders, reflects the final state after unfilled contracts are canceled (e.g. "10.00").
    /// </summary>
    public required string RemainingCount { get; init; }

    /// <summary>
    /// Matching engine timestamp at which the order was processed, as Unix epoch milliseconds.
    /// </summary>
    public required long TsMs { get; init; }

    /// <summary>
    /// Client-provided order ID for correlation, if supplied on the request.
    /// </summary>
    public string? ClientOrderId { get; init; }

    /// <summary>
    /// Volume-weighted average fill price. Only present when <see cref="FillCount"/> &gt; 0 (e.g. "0.5600").
    /// </summary>
    public string? AverageFillPrice { get; init; }

    /// <summary>
    /// Volume-weighted average fee paid per contract for fills resulting from this request.
    /// Only present when <see cref="FillCount"/> &gt; 0 (e.g. "0.5600").
    /// </summary>
    public string? AverageFeePaid { get; init; }
}
