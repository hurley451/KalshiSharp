using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response returned by the List Structured Targets endpoint.
/// </summary>
public sealed record StructuredTargetsResponse : PagedResponse<StructuredTargetResponse>
{
    /// <summary>The targets in this page.</summary>
    public IReadOnlyList<StructuredTargetResponse> StructuredTargets { get; init; } = [];

    /// <inheritdoc />
    public override IReadOnlyList<StructuredTargetResponse> Items => StructuredTargets;
}
