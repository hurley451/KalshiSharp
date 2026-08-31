using System.Text.Json;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response envelope for data returned through the CF Benchmarks passthrough.
/// </summary>
public sealed record CfBenchmarksResponse
{
    /// <summary>
    /// Gets the raw CF Benchmarks response payload without discarding provider-specific fields.
    /// </summary>
    public required JsonElement Data { get; init; }
}
