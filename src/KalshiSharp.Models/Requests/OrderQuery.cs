using System.Globalization;
using KalshiSharp.Models.Common;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Query parameters for listing orders.
/// </summary>
/// <remarks>
/// Supports cursor-based pagination via <see cref="PaginationParameters.Cursor"/>.
/// Use <see cref="PagedResponse{T}.Cursor"/> from the response to fetch the next page.
/// </remarks>
public sealed record OrderQuery : PaginationParameters
{
    /// <summary>
    /// Filter by order status.
    /// </summary>
    public OrderStatus? Status { get; init; }

    /// <summary>
    /// Filter by market ticker.
    /// </summary>
    public string? Ticker { get; init; }

    /// <summary>
    /// Filter by event ticker.
    /// </summary>
    public string? EventTicker { get; init; }

    /// <summary>
    /// Minimum creation time filter.
    /// </summary>
    public DateTimeOffset? MinTs { get; init; }

    /// <summary>
    /// Maximum creation time filter.
    /// </summary>
    public DateTimeOffset? MaxTs { get; init; }

    /// <summary>
    /// Builds the query string for the API request.
    /// </summary>
    /// <returns>The query string including the leading '?' if parameters exist.</returns>
    public string ToQueryString()
    {
        var builder = new QueryStringBuilder();

        AppendPaginationParameters(builder);

        if (Status.HasValue)
        {
            builder.Append("status", Status.Value.ToString().ToLowerInvariant());
        }

        builder.AppendIfNotEmpty("ticker", Ticker);
        builder.AppendIfNotEmpty("event_ticker", EventTicker);

        if (MinTs.HasValue)
        {
            builder.Append("min_ts", MinTs.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }

        if (MaxTs.HasValue)
        {
            builder.Append("max_ts", MaxTs.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }

        return builder.Build();
    }
}
