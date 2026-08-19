using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>
/// Optional query parameters for cancelling a V2 order.
/// </summary>
public sealed record CancelOrderQueryV2
{
    /// <summary>
    /// Subaccount number (0 for primary, 1-63 for subaccounts). Defaults to 0.
    /// </summary>
    public int? Subaccount { get; init; }

    /// <summary>
    /// Identifier for an exchange shard. Defaults to 0 if unspecified.
    /// </summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>
    /// Market ticker used to auto-route the cancellation when <see cref="ExchangeIndex"/> is -1.
    /// </summary>
    public string? MarketTicker { get; init; }

    /// <summary>
    /// Builds the query string for the API request.
    /// </summary>
    /// <returns>The query string including the leading '?' if parameters exist.</returns>
    public string ToQueryString()
    {
        if (ExchangeIndex == -1)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(MarketTicker);
        }

        var builder = new QueryStringBuilder();
        builder.AppendIfNotNull("subaccount", Subaccount);
        builder.AppendIfNotNull("exchange_index", ExchangeIndex);
        builder.AppendIfNotEmpty("market_ticker", MarketTicker);
        return builder.Build();
    }
}
