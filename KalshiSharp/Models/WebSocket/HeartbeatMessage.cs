using System.Text.Json.Serialization;

namespace KalshiSharp.Models.WebSocket;

/// <summary>Legacy heartbeat message sent by the server.</summary>
public sealed record HeartbeatMessage : WebSocketMessage
{
    /// <inheritdoc/>
    public override string Type => "heartbeat";

    /// <summary>Server-side heartbeat identifier.</summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }
}
