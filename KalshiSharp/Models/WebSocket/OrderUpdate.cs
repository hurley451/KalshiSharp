using System.Text.Json.Serialization;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.WebSocket;

/// <summary>
/// Real-time order update from the WebSocket stream.
/// Sent when an order is created, filled, partially filled, or cancelled.
/// </summary>
public sealed record OrderUpdate : WebSocketMessage
{
    /// <inheritdoc/>
    public override string Type => "order";

    /// <summary>
    /// Unique identifier for this order.
    /// </summary>
    [JsonPropertyName("order_id")]
    public required string OrderId { get; init; }

    /// <summary>
    /// Client-provided order identifier for correlation.
    /// </summary>
    [JsonPropertyName("client_order_id")]
    public string? ClientOrderId { get; init; }

    /// <summary>
    /// Market ticker this order is for.
    /// </summary>
    [JsonPropertyName("market_ticker")]
    public required string MarketTicker { get; init; }

    /// <summary>
    /// Order side (Yes or No).
    /// </summary>
    [JsonPropertyName("side")]
    public required OrderSide Side { get; init; }

    /// <summary>
    /// Order type (Limit or Market).
    /// </summary>
    [JsonPropertyName("order_type")]
    public required OrderType OrderType { get; init; }

    /// <summary>
    /// Current status of the order.
    /// </summary>
    [JsonPropertyName("status")]
    public required OrderStatus Status { get; init; }

    /// <summary>
    /// Whether this is a buy or sell action.
    /// </summary>
    [JsonPropertyName("action")]
    public required string Action { get; init; }

    /// <summary>
    /// Total quantity ordered.
    /// </summary>
    [JsonPropertyName("count")]
    public required int Count { get; init; }

    /// <summary>
    /// Quantity remaining (not yet filled).
    /// </summary>
    [JsonPropertyName("remaining_count")]
    public required int RemainingCount { get; init; }

    /// <summary>
    /// Limit price in cents (1-99).
    /// </summary>
    [JsonPropertyName("yes_price")]
    public required int YesPrice { get; init; }

    /// <summary>
    /// No price in cents (derived from yes price).
    /// </summary>
    [JsonPropertyName("no_price")]
    public required int NoPrice { get; init; }

    /// <summary>
    /// Time in force for this order.
    /// </summary>
    [JsonPropertyName("time_in_force")]
    public TimeInForce? TimeInForce { get; init; }

    /// <summary>
    /// When the order was created (Unix milliseconds).
    /// </summary>
    [JsonPropertyName("created_time")]
    public long? CreatedTimeMs { get; init; }

    /// <summary>
    /// Gets the order creation time as a DateTimeOffset.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset? CreatedTime => CreatedTimeMs.HasValue
        ? DateTimeOffset.FromUnixTimeMilliseconds(CreatedTimeMs.Value)
        : null;

    /// <summary>
    /// When the order expires (for GTD orders, Unix milliseconds).
    /// </summary>
    [JsonPropertyName("expiration_time")]
    public long? ExpirationTimeMs { get; init; }

    /// <summary>
    /// Gets the order expiration time as a DateTimeOffset.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset? ExpirationTime => ExpirationTimeMs.HasValue
        ? DateTimeOffset.FromUnixTimeMilliseconds(ExpirationTimeMs.Value)
        : null;

    /// <summary>
    /// Self-trade prevention type.
    /// </summary>
    [JsonPropertyName("self_trade_prevention_type")]
    public string? SelfTradePreventionType { get; init; }


    /// <summary>
    /// Reason for order cancellation or rejection.
    /// </summary>
    [JsonPropertyName("decrease_reason")]
    public string? DecreaseReason { get; init; }

    /// <summary>
    /// Quantity that has been filled.
    /// </summary>
    [JsonIgnore]
    public int FilledCount => Count - RemainingCount;
}
