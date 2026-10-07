namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response containing the caller's public communications ID.
/// </summary>
public sealed record CommunicationsIdResponse
{
    /// <summary>Public communications ID used to identify the user in RFQ and quote workflows.</summary>
    public required string CommunicationsId { get; init; }
}
