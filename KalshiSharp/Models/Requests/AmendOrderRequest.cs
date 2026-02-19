using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Request to amend an existing order on the Kalshi exchange.
/// </summary>
/// <remarks>
/// Only specified fields will be updated. All fields are optional.
/// At least one field must be specified.
/// </remarks>
public sealed record AmendOrderRequest
{
    /// <summary>
    /// Market ticker.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Side of the order.
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// Action of the order
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Updated yes price for the order in cents
    /// </summary>
    public int? YesPrice { get; init; }

    /// <summary>
    /// Update yes price for the order in cents
    /// </summary>
    public int? NoPrice { get; init; }

    /// <summary>
    /// The new quantity (total contracts).
    /// </summary>
    public int? Count { get; init; }
}
