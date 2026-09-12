using KalshiSharp.Models.Common;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Query parameters for listing incentive programs.
/// </summary>
public sealed record IncentiveProgramQuery : PaginationParameters
{
    /// <summary>Filters incentive programs by lifecycle status.</summary>
    public IncentiveProgramStatus? Status { get; init; }

    /// <summary>Filters incentive programs by program type.</summary>
    public IncentiveProgramType? Type { get; init; }

    /// <summary>Filters by the exact incentive description.</summary>
    public string? IncentiveDescription { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    /// <returns>The query string including the leading question mark when parameters exist.</returns>
    public string ToQueryString()
    {
        if (Limit is < 1 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(Limit), Limit, "Limit must be between 1 and 10000.");
        }

        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);

        if (Status.HasValue)
        {
            builder.Append("status", Status.Value switch
            {
                IncentiveProgramStatus.All => "all",
                IncentiveProgramStatus.Active => "active",
                IncentiveProgramStatus.Upcoming => "upcoming",
                IncentiveProgramStatus.Closed => "closed",
                IncentiveProgramStatus.PaidOut => "paid_out",
                _ => throw new ArgumentOutOfRangeException(nameof(Status), Status, "Unsupported incentive status.")
            });
        }

        if (Type.HasValue)
        {
            builder.Append("type", Type.Value switch
            {
                IncentiveProgramType.All => "all",
                IncentiveProgramType.Liquidity => "liquidity",
                IncentiveProgramType.Volume => "volume",
                IncentiveProgramType.MarginMakerVolume => "margin_maker_volume",
                IncentiveProgramType.MarginTakerVolume => "margin_taker_volume",
                _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unsupported incentive type.")
            });
        }

        builder.AppendIfNotEmpty("incentive_description", IncentiveDescription);
        return builder.Build();
    }
}
