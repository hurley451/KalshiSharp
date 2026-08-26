namespace KalshiSharp.Models.Responses;

/// <summary>
/// A rewards program for trading activity on a Kalshi market.
/// </summary>
public sealed record IncentiveProgramResponse
{
    /// <summary>Gets the unique incentive-program identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the unique identifier of the associated market.</summary>
    public required string MarketId { get; init; }

    /// <summary>Gets the ticker of the associated market.</summary>
    public required string MarketTicker { get; init; }

    /// <summary>Gets the associated series ticker when supplied.</summary>
    public string? SeriesTicker { get; init; }

    /// <summary>Gets the program type without restricting future values.</summary>
    public required string IncentiveType { get; init; }

    /// <summary>Gets the program's plain-text description.</summary>
    public required string IncentiveDescription { get; init; }

    /// <summary>Gets when the incentive period starts.</summary>
    public required DateTimeOffset StartDate { get; init; }

    /// <summary>Gets when the incentive period ends.</summary>
    public required DateTimeOffset EndDate { get; init; }

    /// <summary>Gets the total reward for the period in centi-cents.</summary>
    public required long PeriodReward { get; init; }

    /// <summary>Gets whether the incentive has been paid out.</summary>
    public required bool PaidOut { get; init; }

    /// <summary>Gets the optional discount factor in basis points.</summary>
    public int? DiscountFactorBps { get; init; }

    /// <summary>Gets the optional fixed-point target size.</summary>
    public string? TargetSizeFp { get; init; }
}
