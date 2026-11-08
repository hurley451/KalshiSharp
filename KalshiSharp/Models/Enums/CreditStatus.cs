namespace KalshiSharp.Models.Enums;

/// <summary>Processing status of a portfolio credit.</summary>
public enum CreditStatus
{
    /// <summary>The status is not yet known.</summary>
    Unknown,

    /// <summary>The credit is awaiting processing.</summary>
    Pending,

    /// <summary>The credit could not be applied.</summary>
    Failed,

    /// <summary>The credit has been applied.</summary>
    Applied
}
