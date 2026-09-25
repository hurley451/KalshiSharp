namespace KalshiSharp.Models.Enums;

/// <summary>
/// How automatic target-balance rebalancing should reserve collateral for resting orders.
/// </summary>
public enum RestingMarginReservation
{
    /// <summary>Reserve no collateral for resting orders.</summary>
    None,

    /// <summary>Reserve the largest single market-side resting-order commitment.</summary>
    Max,

    /// <summary>Reserve all resting-order margin. This is Kalshi's default when omitted.</summary>
    Sum
}
