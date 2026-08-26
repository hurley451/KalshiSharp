namespace KalshiSharp.Models.Enums;

/// <summary>
/// Status filters supported by the incentive-program listing API.
/// </summary>
public enum IncentiveProgramStatus
{
    /// <summary>Return incentive programs in every status.</summary>
    All,

    /// <summary>Return currently active incentive programs.</summary>
    Active,

    /// <summary>Return incentive programs that have not started.</summary>
    Upcoming,

    /// <summary>Return closed incentive programs.</summary>
    Closed,

    /// <summary>Return incentive programs whose rewards have been paid.</summary>
    PaidOut
}
