namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents the user's account balance.
/// </summary>
public sealed record BalanceResponse
{
    /// <summary>
    /// Member's available balance in cents. This represents the amount available for trading.
    /// </summary>
    public required long Balance { get; init; }

    /// <summary>
    /// Member's portfolio value in cents. This is the current value of all positions held.
    /// </summary>
    public required long PortfolioValue { get; init; }

    /// <summary>
    /// Unix timestamp of the last update to the balance.
    /// </summary>
    public required long UpdatedTs { get; init; }
}
