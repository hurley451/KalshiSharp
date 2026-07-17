using KalshiSharp.Models.Enums;
using System.Text.Json.Serialization;

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
    /// Updated yes price for the order in dollars
    /// </summary>
    [JsonPropertyName("yes_price_dollars")]
    public string? YesPriceDollars { get; init; }

    /// <summary>
    /// Updated no price for the order in dollars
    /// </summary>
    [JsonPropertyName("no_price_dollars")]
    public string? NoPriceDollars { get; init; }

    /// <summary>
    /// The new quantity (total contracts) - Fixed-point (2 decimals).
    /// </summary>
    [JsonPropertyName("count_fp")]
    public string? CountFp { get; init; }
}
