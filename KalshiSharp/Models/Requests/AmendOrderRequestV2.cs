using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Request to amend an existing order via the V2 events orders endpoint.
/// </summary>
public sealed record AmendOrderRequestV2
{
    /// <summary>
    /// The market ticker of the order to amend.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Side of the order.
    /// </summary>
    public required OrderBookSide Side { get; init; }

    /// <summary>
    /// Updated price for the order in fixed-point dollars (e.g. "0.5600").
    /// </summary>
    public required string Price { get; init; }

    /// <summary>
    /// Updated total/max fillable count for the order.
    /// Set to the order's already filled count plus the desired resting remaining count after the amend (e.g. "10.00").
    /// </summary>
    public required string Count { get; init; }

    /// <summary>
    /// The original client-specified order ID to be amended.
    /// </summary>
    public string? ClientOrderId { get; init; }

    /// <summary>
    /// The new client-specified order ID after amendment.
    /// </summary>
    public string? UpdatedClientOrderId { get; init; }

    /// <summary>
    /// Identifier for an exchange shard. Defaults to 0 if unspecified.
    /// </summary>
    public int? ExchangeIndex { get; init; }
}
