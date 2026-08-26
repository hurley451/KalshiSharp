namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response for listing incentive programs.
/// </summary>
public sealed record IncentiveProgramsResponse
{
    /// <summary>Gets the incentive programs in this page.</summary>
    public IReadOnlyList<IncentiveProgramResponse> IncentivePrograms { get; init; } = [];

    /// <summary>Gets the cursor returned by the API for the next page.</summary>
    public string? NextCursor { get; init; }

    /// <summary>Gets the items in this page.</summary>
    public IReadOnlyList<IncentiveProgramResponse> Items => IncentivePrograms;

    /// <summary>Gets the cursor to pass to the next request.</summary>
    public string? Cursor => NextCursor;

    /// <summary>Gets whether another non-empty page may be available.</summary>
    public bool HasMore => !string.IsNullOrEmpty(NextCursor) && IncentivePrograms.Count > 0;
}
