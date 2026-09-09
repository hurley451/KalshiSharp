namespace KalshiSharp.Models.Responses;

/// <summary>
/// Wrapper returned by the Get Structured Target endpoint.
/// </summary>
public sealed record SingleStructuredTargetResponse
{
    /// <summary>The target when supplied by the API.</summary>
    public StructuredTargetResponse? StructuredTarget { get; init; }
}
