using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Responses;

/// <summary>Response for listing multivariate event collections.</summary>
public sealed record MultivariateEventCollectionsResponse : PagedResponse<MultivariateEventCollectionResponse>
{
    /// <summary>Multivariate event collections in the current page.</summary>
    public IReadOnlyList<MultivariateEventCollectionResponse> MultivariateContracts { get; init; } = [];

    /// <inheritdoc />
    public override IReadOnlyList<MultivariateEventCollectionResponse> Items => MultivariateContracts;
}

/// <summary>Response for retrieving one multivariate event collection.</summary>
public sealed record SingleMultivariateEventCollectionResponse
{
    /// <summary>The requested multivariate event collection.</summary>
    public MultivariateEventCollectionResponse? MultivariateContract { get; init; }
}

/// <summary>A multivariate event collection.</summary>
public sealed record MultivariateEventCollectionResponse
{
    /// <summary>Unique collection ticker.</summary>
    public string? CollectionTicker { get; init; }

    /// <summary>Series associated with the collection.</summary>
    public string? SeriesTicker { get; init; }

    /// <summary>Exchange shard inherited from the collection's series.</summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>Collection title.</summary>
    public string? Title { get; init; }

    /// <summary>Short collection description.</summary>
    public string? Description { get; init; }

    /// <summary>When the collection opens for interaction.</summary>
    public DateTimeOffset? OpenDate { get; init; }

    /// <summary>When the collection closes for interaction.</summary>
    public DateTimeOffset? CloseDate { get; init; }

    /// <summary>Events that can provide inputs for this collection.</summary>
    public IReadOnlyList<MultivariateAssociatedEventResponse> AssociatedEvents { get; init; } = [];

    /// <summary>Deprecated event ticker list retained by the wire contract.</summary>
    public IReadOnlyList<string> AssociatedEventTickers { get; init; } = [];

    /// <summary>Whether selected markets are order-sensitive.</summary>
    public bool? IsOrdered { get; init; }

    /// <summary>Deprecated single-market-per-event flag retained by the wire contract.</summary>
    public bool? IsSingleMarketPerEvent { get; init; }

    /// <summary>Deprecated all-YES-side flag retained by the wire contract.</summary>
    public bool? IsAllYes { get; init; }

    /// <summary>Minimum number of selected markets accepted by lookup/create.</summary>
    public int? SizeMin { get; init; }

    /// <summary>Maximum number of selected markets accepted by lookup/create.</summary>
    public int? SizeMax { get; init; }

    /// <summary>Functional description for how selected markets affect the output.</summary>
    public string? FunctionalDescription { get; init; }

    /// <summary>Price-level structure used for newly created markets in this collection.</summary>
    public string? PriceLevelStructure { get; init; }

    /// <summary>Dynamic fixed-point price bands used for newly created markets in this collection.</summary>
    public IReadOnlyList<MarketResponse.PriceRange> PriceRanges { get; init; } = [];
}

/// <summary>An event accepted as an input to a multivariate event collection.</summary>
public sealed record MultivariateAssociatedEventResponse
{
    /// <summary>Event ticker.</summary>
    public string? Ticker { get; init; }

    /// <summary>Whether only the YES side can be selected for this event.</summary>
    public bool? IsYesOnly { get; init; }

    /// <summary>Maximum number of markets from this event, or null for no limit.</summary>
    public int? SizeMax { get; init; }

    /// <summary>Minimum number of markets from this event, or null for no limit.</summary>
    public int? SizeMin { get; init; }

    /// <summary>Active public communications IDs quoting this event.</summary>
    public IReadOnlyList<string> ActiveQuoters { get; init; } = [];
}
