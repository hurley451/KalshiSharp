namespace KalshiSharp.Models.Responses;

/// <summary>
/// Wrapper returned by the Get Series endpoint.
/// </summary>
public sealed record SingleSeriesResponse
{
    /// <summary>Series details.</summary>
    public required SeriesResponse Series { get; init; }
}
