namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents the user's account balance.
/// </summary>
public sealed record BalanceResponse
{
    /// <summary>
    /// Total account balance in cents.
    /// </summary>
    public required long Balance { get; init; }

    /// <summary>
    /// Available balance for trading in cents (excludes collateral).
    /// </summary>
    public required long AvailableBalance { get; init; }

    /// <summary>
    /// Balance currently held as collateral for open positions.
    /// </summary>
    public long? PortfolioBalance { get; init; }

    /// <summary>
    /// Pending deposits in cents.
    /// </summary>
    public long? PendingDeposits { get; init; }

    /// <summary>
    /// Pending withdrawals in cents.
    /// </summary>
    public long? PendingWithdrawals { get; init; }

    /// <summary>
    /// Total payout from settled positions.
    /// </summary>
    public long? TotalDeposits { get; init; }

    /// <summary>
    /// Total withdrawn from account.
    /// </summary>
    public long? TotalWithdrawals { get; init; }
}
