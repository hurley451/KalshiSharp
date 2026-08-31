using System.Text.Json.Serialization;

namespace KalshiSharp.Models.WebSocket;

/// <summary>Real-time CF Benchmarks index value update.</summary>
public sealed record CfBenchmarksValueUpdate : WebSocketMessage<CfBenchmarksValueUpdate.MessageBody>
{
    /// <inheritdoc />
    public override string Type => "cfbenchmarks_value";

    /// <summary>CF Benchmarks value payload.</summary>
    public sealed record MessageBody
    {
        /// <summary>Index identifier, such as <c>BRTI</c>.</summary>
        [JsonPropertyName("index_id")]
        public required string IndexId { get; init; }

        /// <summary>Unix timestamp in milliseconds when Kalshi received the value.</summary>
        [JsonPropertyName("received_at")]
        public required long ReceivedAt { get; init; }

        /// <summary>Raw upstream CF Benchmarks JSON frame.</summary>
        public required string Data { get; init; }

        /// <summary>Trailing 60-second average, always supplied by the channel.</summary>
        [JsonPropertyName("avg_60s_data")]
        public required WindowAverage Average60Seconds { get; init; }

        /// <summary>Quarter-hour final-minute average when the update falls in that window.</summary>
        [JsonPropertyName("last_60s_windowed_average_15min")]
        public WindowAverage? Last60SecondsWindowedAverage15Minutes { get; init; }

        /// <summary>Gets <see cref="ReceivedAt"/> as UTC.</summary>
        [JsonIgnore]
        public DateTimeOffset ReceivedAtUtc => DateTimeOffset.FromUnixTimeMilliseconds(ReceivedAt);
    }

    /// <summary>Average and source-window metadata.</summary>
    public sealed record WindowAverage
    {
        /// <summary>Average value exactly as supplied by CF Benchmarks.</summary>
        public required string Value { get; init; }

        /// <summary>Number of values included in the window.</summary>
        [JsonPropertyName("window_size")]
        public required int WindowSize { get; init; }

        /// <summary>Window start boundary in Unix milliseconds.</summary>
        [JsonPropertyName("window_start_ts_ms")]
        public required long WindowStartTimestampMs { get; init; }

        /// <summary>Window end boundary in Unix milliseconds.</summary>
        [JsonPropertyName("window_end_ts_exclusive")]
        public required long WindowEndTimestampExclusive { get; init; }
    }
}

/// <summary>Available CF Benchmarks index identifiers returned by an index-list command.</summary>
public sealed record CfBenchmarksIndexList : WebSocketMessage<CfBenchmarksIndexList.MessageBody>
{
    /// <inheritdoc />
    public override string Type => "cfbenchmarks_value_indexlist";

    /// <summary>Client command identifier when supplied by the server.</summary>
    public int? Id { get; init; }

    /// <summary>Index-list payload.</summary>
    public sealed record MessageBody
    {
        /// <summary>Available CF Benchmarks index identifiers.</summary>
        [JsonPropertyName("index_ids")]
        public required IReadOnlyList<string> IndexIds { get; init; }
    }
}
