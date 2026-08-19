using System.Text.Json;
using System.Text.Json.Serialization;
using KalshiSharp.Models.WebSocket;

namespace KalshiSharp.Serialization.Converters;

/// <summary>Dispatches WebSocket envelopes while supporting legacy and current error shapes.</summary>
public sealed class WebSocketMessageConverter : JsonConverter<WebSocketMessage>
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(WebSocketMessage);

    /// <inheritdoc />
    public override WebSocketMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var type = root.TryGetProperty("type", out var typeElement)
            ? typeElement.GetString()
            : null;

        var targetType = type switch
        {
            "orderbook_delta" => typeof(OrderBookUpdate),
            "orderbook_snapshot" => typeof(OrderBookSnapshot),
            "trade" => typeof(TradeUpdate),
            "heartbeat" => typeof(HeartbeatMessage),
            "order" => typeof(OrderUpdate),
            "user_order" => typeof(UserOrderUpdate),
            "fill" => typeof(FillUpdate),
            "market_position" => typeof(MarketPositionUpdate),
            "subscribed" => typeof(SubscriptionConfirmation),
            "unsubscribed" => typeof(UnsubscriptionConfirmation),
            "error" => ResolveErrorType(root),
            "ok" => typeof(OKMessage),
            "ticker" => typeof(TickerUpdate),
            "market_lifecycle_v2" => typeof(MarketLifecycleUpdate),
            "multivariate_market_lifecycle" => typeof(MultivariateMarketLifecycleUpdate),
            "event_lifecycle" => typeof(EventLifecycleUpdate),
            "event_fee_update" => typeof(EventFeeUpdate),
            _ => null
        };

        if (targetType is null)
        {
            return UnknownMessage.Create(type ?? "unknown", root.Clone());
        }

        return (WebSocketMessage?)JsonSerializer.Deserialize(root.GetRawText(), targetType, options);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, WebSocketMessage value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, value.GetType(), options);

    private static Type ResolveErrorType(JsonElement root) =>
        root.TryGetProperty("msg", out var message) && message.ValueKind == JsonValueKind.Object
            ? typeof(ErrorMessageV2)
            : typeof(ErrorMessage);
}
