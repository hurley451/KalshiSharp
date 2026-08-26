namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response returned by the List Series endpoint.
/// </summary>
public sealed record SeriesListResponse
{
    /// <summary>The matching series.</summary>
    public required IReadOnlyList<SeriesResponse> Series { get; init; }

    /// <summary>The matching series.</summary>
    public IReadOnlyList<SeriesResponse> Items => Series;
}
