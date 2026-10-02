using System.Text.Json;
using System.Text.Json.Serialization;

namespace KalshiSharp.Models.WebSocket;

/// <summary>
/// Represents an unknown or unrecognized WebSocket message type.
/// Allows passthrough of messages not yet implemented in the SDK.
/// </summary>
public sealed record UnknownMessage : WebSocketMessage
{
    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public override string Type { get; } = "unknown";

    /// <summary>
    /// The raw message type received from the server.
    /// </summary>
    public string RawType { get; init; } = string.Empty;

    /// <summary>
    /// The raw JSON payload for inspection or custom parsing.
    /// </summary>
    public JsonElement? RawPayload { get; init; }

    /// <summary>
    /// Creates an UnknownMessage from a raw type and payload.
    /// </summary>
    public static UnknownMessage Create(string rawType, JsonElement payload) => new()
    {
        RawType = rawType,
        RawPayload = payload,
        Sid = TryGetInt32(payload, "sid") ?? 0,
        Sequence = TryGetInt64(payload, "seq"),
        Timestamp = TryGetInt64(payload, "ts"),
        SendingTsMs = TryGetInt64(payload, "sending_ts_ms")
    };

    private static int? TryGetInt32(JsonElement payload, string propertyName) =>
        payload.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result)
            ? result
            : null;

    private static long? TryGetInt64(JsonElement payload, string propertyName) =>
        payload.TryGetProperty(propertyName, out var value) && value.TryGetInt64(out var result)
            ? result
            : null;
}
