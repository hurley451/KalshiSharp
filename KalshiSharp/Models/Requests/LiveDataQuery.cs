using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Query parameters for milestone live-data endpoints.</summary>
public sealed record LiveDataQuery
{
    /// <summary>Include player-level statistics when supported by the milestone.</summary>
    public bool? IncludePlayerStats { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        var builder = new QueryStringBuilder();
        if (IncludePlayerStats.HasValue)
        {
            builder.Append("include_player_stats", IncludePlayerStats.Value ? "true" : "false");
        }

        return builder.Build();
    }
}
