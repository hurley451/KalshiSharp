using KalshiSharp.Models.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response for listing trades.
/// </summary>
public sealed record TradesResponse : PagedResponse<TradeResponse>
{
    private IReadOnlyList<TradeResponse> _trades = [];

    /// <summary>
    /// The trades in this page.
    /// </summary>
    [JsonRequired]
    public IReadOnlyList<TradeResponse> Trades
    {
        get => _trades;
        init => _trades = value ?? throw new JsonException("The trades collection must not be null.");
    }

    /// <inheritdoc />
    public override IReadOnlyList<TradeResponse> Items => Trades;
}
