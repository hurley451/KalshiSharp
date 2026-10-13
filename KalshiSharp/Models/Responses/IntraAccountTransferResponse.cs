namespace KalshiSharp.Models.Responses;

/// <summary>Response for an accepted intra-account transfer request.</summary>
public sealed record IntraAccountTransferResponse
{
    /// <summary>Kalshi transfer identifier.</summary>
    public required string TransferId { get; init; }
}
