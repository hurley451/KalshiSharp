using KalshiSharp.Models.Enums;
using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Request to create a new order on the Kalshi exchange.
/// </summary>
public sealed record CreateOrderRequest
{
    /// <summary>
    /// The market ticker to place the order on.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// The side of the order (Yes or No).
    /// </summary>
    public required OrderSide Side { get; init; }

    /// <summary>
    /// The action to take (buy or sell).
    /// </summary>
    public required string Action { get; init; }   

    /// <summary>
    /// The number of contracts to buy/sell - Fixed-point (2 decimals).
    /// </summary>
    [JsonPropertyName("count_fp")]
    public required string CountFp { get; init; }

    /// <summary>
    /// The limit price in dollars. Required for limit orders on Yes side.
    /// </summary>
    [JsonPropertyName("yes_price_dollars")]
    public string? YesPriceDollars { get; init; }

    /// <summary>
    /// The no price in dollars. Required for limit orders on No side.
    /// </summary>
    [JsonPropertyName("no_price_dollars")]
    public string? NoPriceDollars { get; init; }

    /// <summary>
    /// The order type (Limit or Market).
    /// </summary>
    public required OrderType Type { get; init; }

    /// <summary>
    /// Time in force for the order. Defaults to GTC.
    /// </summary>
    public TimeInForce? TimeInForce { get; init; }

    /// <summary>
    /// Expiration time for GTD orders.
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; init; }

    /// <summary>
    /// Optional client-provided order ID for correlation.
    /// </summary>
    public string? ClientOrderId { get; init; }

    /// <summary>
    /// If true, the order will only be placed if it can rest on the book (maker only).
    /// </summary>
    public bool? SellPositionFloor { get; init; }

    /// <summary>
    /// If true, the order will only be placed if it can rest on the book (maker only).
    /// </summary>
    public bool? BuyMaxCost { get; init; }
}
