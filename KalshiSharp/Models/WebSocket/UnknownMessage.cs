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
    public override string MessageType { get; } = "unknown";

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
        RawPayload = payload
    };
}
