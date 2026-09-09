using System.Text.Json;
using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents a Kalshi structured target.
/// </summary>
public sealed record StructuredTargetResponse
{
    /// <summary>Unique structured-target ID when supplied.</summary>
    public string? Id { get; init; }

    /// <summary>Human-readable target name when supplied.</summary>
    public string? Name { get; init; }

    /// <summary>Target type when supplied.</summary>
    public string? Type { get; init; }

    /// <summary>Type-specific target details.</summary>
    public StructuredTargetDetails? Details { get; init; }

    /// <summary>Primary external provider ID when supplied.</summary>
    public string? SourceId { get; init; }

    /// <summary>External provider IDs keyed by source name.</summary>
    public IReadOnlyDictionary<string, string>? SourceIds { get; init; }

    /// <summary>When the target was last updated.</summary>
    public DateTimeOffset? LastUpdatedTs { get; init; }
}

/// <summary>
/// Type-specific structured-target details with forward-compatible field preservation.
/// </summary>
public sealed record StructuredTargetDetails
{
    /// <summary>Public target image URL when supplied.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Additional target-type-specific detail fields.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
