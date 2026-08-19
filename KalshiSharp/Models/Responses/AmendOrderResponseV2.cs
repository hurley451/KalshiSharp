namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response returned after successfully amending a V2 order.
/// </summary>
public sealed record AmendOrderResponseV2
{
    /// <summary>
    /// Unique identifier of the amended order.
    /// </summary>
    public required string OrderId { get; init; }

    /// <summary>
    /// Matching engine timestamp at which the amend was processed, as Unix epoch milliseconds.
    /// </summary>
    public required long TsMs { get; init; }

    /// <summary>
    /// Client-provided order ID for correlation, if present after amendment.
    /// </summary>
    public string? ClientOrderId { get; init; }

    /// <summary>
    /// Number of resting contracts remaining after the amend.
    /// Only present when the amend caused a fill or changed the resting size (e.g. "10.00").
    /// </summary>
    public string? RemainingCount { get; init; }

    /// <summary>
    /// Number of contracts filled as a result of the amend crossing the book.
    /// Only present when fills occurred or remaining size changed (e.g. "10.00").
    /// </summary>
    public string? FillCount { get; init; }

    /// <summary>
    /// Volume-weighted average fill price for fills resulting from the amend.
    /// Only present when fills occurred (e.g. "0.5600").
    /// </summary>
    public string? AverageFillPrice { get; init; }

    /// <summary>
    /// Volume-weighted average fee paid per contract for fills resulting from the amend.
    /// Only present when fills occurred (e.g. "0.5600").
    /// </summary>
    public string? AverageFeePaid { get; init; }
}
