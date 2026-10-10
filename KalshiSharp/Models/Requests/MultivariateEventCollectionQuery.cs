using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Query parameters for listing multivariate event collections.</summary>
public sealed record MultivariateEventCollectionQuery : PaginationParameters
{
    /// <summary>Only return collections of a certain status, such as <c>unopened</c>, <c>open</c>, or <c>closed</c>.</summary>
    public string? Status { get; init; }

    /// <summary>Only return collections associated with this event ticker.</summary>
    public string? AssociatedEventTicker { get; init; }

    /// <summary>Only return collections associated with this series ticker.</summary>
    public string? SeriesTicker { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        if (Limit is < 1 or > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(Limit), Limit, "Limit must be between 1 and 200.");
        }

        var builder = new QueryStringBuilder();
        AppendPaginationParameters(builder);
        builder.AppendIfNotEmpty("status", Status);
        builder.AppendIfNotEmpty("associated_event_ticker", AssociatedEventTicker);
        builder.AppendIfNotEmpty("series_ticker", SeriesTicker);
        return builder.Build();
    }
}
