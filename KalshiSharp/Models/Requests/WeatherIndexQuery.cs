using System.Globalization;
using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Query parameters for the weather index endpoint.
/// </summary>
public sealed record WeatherIndexQuery
{
    /// <summary>Window start in Unix milliseconds, inclusive.</summary>
    public long? From { get; init; }

    /// <summary>Window end in Unix milliseconds, inclusive.</summary>
    public long? To { get; init; }

    /// <summary>Trailing window in seconds. Mutually exclusive with <see cref="From"/> and <see cref="To"/>.</summary>
    public long? LastSec { get; init; }

    /// <summary>Whether to include per-station audit readings on each point.</summary>
    public bool? Detailed { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    /// <returns>The query string including the leading question mark when parameters exist.</returns>
    public string ToQueryString()
    {
        if (LastSec.HasValue && (From.HasValue || To.HasValue))
        {
            throw new ArgumentException("last_sec is mutually exclusive with from and to.", nameof(LastSec));
        }

        if (From.HasValue != To.HasValue)
        {
            throw new ArgumentException("from and to must be supplied together unless last_sec is used.", nameof(From));
        }

        if (LastSec is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(LastSec), LastSec, "last_sec must be positive.");
        }

        var builder = new QueryStringBuilder();
        if (From.HasValue)
        {
            builder.Append("from", From.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (To.HasValue)
        {
            builder.Append("to", To.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (LastSec.HasValue)
        {
            builder.Append("last_sec", LastSec.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (Detailed.HasValue)
        {
            builder.Append("detailed", Detailed.Value ? "true" : "false");
        }

        return builder.Build();
    }
}
