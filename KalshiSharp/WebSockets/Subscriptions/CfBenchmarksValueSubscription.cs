namespace KalshiSharp.WebSockets.Subscriptions;

/// <summary>Subscription for real-time CF Benchmarks index values.</summary>
public sealed record CfBenchmarksValueSubscription : WebSocketSubscription
{
    /// <summary>CF Benchmarks channel identifier.</summary>
    public const string ChannelName = "cfbenchmarks_value";

    /// <inheritdoc />
    public override string Channel => ChannelName;

    /// <summary>
    /// Gets the index identifiers to seed, or an empty list to subscribe before adding indices.
    /// Use <c>all</c> to receive every available index.
    /// </summary>
    public IReadOnlyList<string> IndexIds { get; init; } = [];

    /// <summary>Creates a subscription seeded with the supplied index identifiers.</summary>
    public static CfBenchmarksValueSubscription ForIndices(params string[] indexIds) =>
        new() { IndexIds = indexIds };

    /// <summary>Creates a subscription seeded with the supplied index identifiers.</summary>
    public static CfBenchmarksValueSubscription ForIndices(IEnumerable<string> indexIds) =>
        new() { IndexIds = indexIds?.ToList() ?? throw new ArgumentNullException(nameof(indexIds)) };

    /// <summary>Creates a subscription for every available index.</summary>
    public static CfBenchmarksValueSubscription ForAllIndices() =>
        new() { IndexIds = ["all"] };

    /// <inheritdoc />
    internal override SubscriptionParams CreateSubscribeParams()
    {
        ValidateIndexIds(IndexIds, allowEmpty: true);

        return new SubscriptionParams
        {
            Channels = [ChannelName],
            IndexIds = IndexIds.Count == 0 ? null : IndexIds
        };
    }

    internal static void ValidateIndexIds(IReadOnlyList<string> indexIds, bool allowEmpty)
    {
        ArgumentNullException.ThrowIfNull(indexIds);

        if (!allowEmpty && indexIds.Count == 0)
        {
            throw new ArgumentException("At least one index identifier is required.", nameof(indexIds));
        }

        if (indexIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Index identifiers cannot be empty.", nameof(indexIds));
        }

        if (indexIds.Count > 1 && indexIds.Any(id => string.Equals(id, "all", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("The all index identifier cannot be combined with other identifiers.", nameof(indexIds));
        }
    }
}
