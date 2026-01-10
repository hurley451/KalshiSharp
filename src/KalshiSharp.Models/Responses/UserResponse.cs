namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents the authenticated user's profile.
/// </summary>
public sealed record UserResponse
{
    /// <summary>
    /// Unique user identifier.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// User's username/handle.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// When the user account was created.
    /// </summary>
    public DateTimeOffset? CreatedTime { get; init; }

    /// <summary>
    /// User's first name.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// User's last name.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// User's verification status.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Whether the user is verified for trading.
    /// </summary>
    public bool? IsVerified { get; init; }

    /// <summary>
    /// Trading tier/level.
    /// </summary>
    public string? TradingTier { get; init; }
}
