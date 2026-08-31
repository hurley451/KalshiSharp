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
            "cfbenchmarks_value" => typeof(CfBenchmarksValueUpdate),
            "cfbenchmarks_value_indexlist" => typeof(CfBenchmarksIndexList),
            _ => null
        };

        if (targetType is null)
        {
            return UnknownMessage.Create(type ?? "unknown", root.Clone());
        }

        if (type is "cfbenchmarks_value" or "cfbenchmarks_value_indexlist")
        {
            ValidateCfBenchmarksEnvelope(root, type);
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

    private static void ValidateCfBenchmarksEnvelope(JsonElement root, string type)
    {
        if (!root.TryGetProperty("sid", out var sid) ||
            !sid.TryGetInt32(out var subscriptionId) ||
            subscriptionId <= 0)
        {
            throw new JsonException("CF Benchmarks messages require a positive sid.");
        }

        if (!root.TryGetProperty("seq", out var sequence) ||
            !sequence.TryGetInt64(out var sequenceNumber) ||
            sequenceNumber <= 0)
        {
            throw new JsonException("CF Benchmarks messages require a positive seq.");
        }

        if (root.TryGetProperty("id", out var commandId) &&
            (!commandId.TryGetInt32(out var commandIdValue) || commandIdValue < 0))
        {
            throw new JsonException("CF Benchmarks command IDs cannot be negative.");
        }

        if (!root.TryGetProperty("msg", out var message) || message.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("CF Benchmarks messages require an object msg payload.");
        }

        if (type == "cfbenchmarks_value")
        {
            RequireString(message, "index_id");
            RequireInteger(message, "received_at");
            RequireString(message, "data");
            var average = RequireObject(message, "avg_60s_data");
            ValidateWindowAverage(average);

            if (message.TryGetProperty("last_60s_windowed_average_15min", out var quarterHourAverage))
            {
                if (quarterHourAverage.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException(
                        "CF Benchmarks last_60s_windowed_average_15min must be an object when present.");
                }

                ValidateWindowAverage(quarterHourAverage);
            }

            return;
        }

        if (!message.TryGetProperty("index_ids", out var indexIds) ||
            indexIds.ValueKind != JsonValueKind.Array ||
            indexIds.EnumerateArray().Any(indexId => indexId.ValueKind != JsonValueKind.String))
        {
            throw new JsonException("CF Benchmarks index-list messages require string index_ids.");
        }
    }

    private static void ValidateWindowAverage(JsonElement average)
    {
        RequireString(average, "value");
        RequireInteger(average, "window_size");
        RequireInteger(average, "window_start_ts_ms");
        RequireInteger(average, "window_end_ts_exclusive");
    }

    private static JsonElement RequireObject(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"CF Benchmarks {propertyName} must be an object.");
        }

        return value;
    }

    private static void RequireString(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"CF Benchmarks {propertyName} must be a string.");
        }
    }

    private static void RequireInteger(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value) || !value.TryGetInt64(out _))
        {
            throw new JsonException($"CF Benchmarks {propertyName} must be an integer.");
        }
    }
}
