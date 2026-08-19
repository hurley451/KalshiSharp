using System.Globalization;
using KalshiSharp.Models.Common;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Query parameters for listing markets.
/// </summary>
/// <remarks>
/// Supports cursor-based pagination via <see cref="PaginationParameters.Cursor"/>.
/// Use <see cref="PagedResponse{T}.Cursor"/> from the response to fetch the next page.
/// </remarks>
public sealed record MarketQuery : PaginationParameters
{
    /// <summary>
    /// Filter by market status.
    /// </summary>
    public MarketStatus? Status { get; init; }

    /// <summary>
    /// Filter by event ticker.
    /// </summary>
    public string? EventTicker { get; init; }

    /// <summary>
    /// Filter by series ticker.
    /// </summary>
    public string? SeriesTicker { get; init; }

    /// <summary>
    /// Filter by market tickers (comma-separated or multiple).
    /// </summary>
    public IReadOnlyList<string>? Tickers { get; init; }

    /// <summary>
    /// Minimum close time filter.
    /// </summary>
    public DateTimeOffset? MinCloseTs { get; init; }

    /// <summary>
    /// Maximum close time filter.
    /// </summary>
    public DateTimeOffset? MaxCloseTs { get; init; }

    /// <summary>
    /// Only returns markets updated at or after this time.
    /// </summary>
    public DateTimeOffset? MinUpdatedTs { get; init; }

    /// <summary>
    /// Filter for multivariate events (combos/parlays).
    /// Use "exclude" to filter out MVE markets, "only" to return only MVE markets.
    /// </summary>
    public string? MveFilter { get; init; }

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
            // API query params use different values than response data:
            // Query: unopened, open, closed, settled
            // Response: initialized, active, closed, finalized
            var statusFilter = Status.Value switch
            {
                MarketStatus.Initialized => "unopened",
                MarketStatus.Active => "open",
                MarketStatus.Closed => "closed",
                MarketStatus.Finalized => "settled",
                _ => Status.Value.ToString().ToLowerInvariant()
            };
            builder.Append("status", statusFilter);
        }

        builder.AppendIfNotEmpty("event_ticker", EventTicker);
        builder.AppendIfNotEmpty("series_ticker", SeriesTicker);

        if (Tickers is { Count: > 0 })
        {
            foreach (var ticker in Tickers)
            {
                builder.Append("tickers", ticker);
            }
        }

        if (MinCloseTs.HasValue)
        {
            builder.Append("min_close_ts", MinCloseTs.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }

        if (MaxCloseTs.HasValue)
        {
            builder.Append("max_close_ts", MaxCloseTs.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }

        if (MinUpdatedTs.HasValue)
        {
            builder.Append("min_updated_ts", MinUpdatedTs.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        }

        builder.AppendIfNotEmpty("mve_filter", MveFilter);

        return builder.Build();
    }
}
