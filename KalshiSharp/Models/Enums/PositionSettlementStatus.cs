namespace KalshiSharp.Models.Enums;

/// <summary>
/// Settlement-state filters supported by the live positions endpoint.
/// </summary>
public enum PositionSettlementStatus
{
    /// <summary>Return unsettled live positions. This is Kalshi's default when omitted.</summary>
    Unsettled,

    /// <summary>Return settled positions that have not yet moved to historical positions.</summary>
    Settled,

    /// <summary>Return both unsettled and settled positions still in the live positions data set.</summary>
    All
}
