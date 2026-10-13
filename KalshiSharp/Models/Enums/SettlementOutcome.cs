namespace KalshiSharp.Models.Enums;

/// <summary>Resolved event-market settlement outcome.</summary>
public enum SettlementOutcome
{
    /// <summary>YES outcome won.</summary>
    Yes,
    /// <summary>NO outcome won.</summary>
    No,
    /// <summary>A scalar settlement value determined the payout.</summary>
    Scalar
}
