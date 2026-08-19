using System.Globalization;
using KalshiSharp.Models.Common;

namespace KalshiSharp.Models.Requests;

/// <summary>Query parameters for an explicitly scoped portfolio balance.</summary>
public sealed record BalanceQuery
{
    /// <summary>Subaccount number, including zero for the primary account.</summary>
    public int? Subaccount { get; init; }

    /// <summary>Exchange shard to query.</summary>
    public int? ExchangeIndex { get; init; }

    /// <summary>Builds the encoded query string.</summary>
    public string ToQueryString()
    {
        var builder = new QueryStringBuilder();
        if (Subaccount.HasValue)
        {
            builder.Append("subaccount", Subaccount.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (ExchangeIndex.HasValue)
        {
            builder.Append("exchange_index", ExchangeIndex.Value.ToString(CultureInfo.InvariantCulture));
        }

        return builder.Build();
    }
}
