namespace KalshiSharp.Models.Enums;

/// <summary>
/// Type filters supported by the Predictions incentive-program API.
/// </summary>
public enum IncentiveProgramType
{
    /// <summary>Return incentive programs of every supported type.</summary>
    All,

    /// <summary>Return liquidity incentive programs.</summary>
    Liquidity,

    /// <summary>Return volume incentive programs.</summary>
    Volume,

    /// <summary>Return Margin maker-volume incentive programs.</summary>
    MarginMakerVolume,

    /// <summary>Return Margin taker-volume incentive programs.</summary>
    MarginTakerVolume
}
