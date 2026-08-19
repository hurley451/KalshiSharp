namespace KalshiSharp.Models.Responses;

/// <summary>
/// Represents the current status of the Kalshi exchange.
/// </summary>
public sealed record ExchangeStatusResponse
{
    /// <summary>
    /// Whether the exchange is currently active and accepting orders.
    /// </summary>
    public required bool ExchangeActive { get; init; }

    /// <summary>
    /// Whether trading is currently enabled.
    /// </summary>
    public required bool TradingActive { get; init; }

    /// <summary>Whether transfers between exchange instances are permitted.</summary>
    public bool? IntraExchangeTransfersActive { get; init; }

    /// <summary>Status of each exchange shard.</summary>
    public IReadOnlyList<ExchangeIndexStatus> ExchangeIndexStatuses { get; init; } = [];

    /// <summary>Status for one exchange shard.</summary>
    public sealed record ExchangeIndexStatus
    {
        /// <summary>Exchange shard identifier.</summary>
        public required int ExchangeIndex { get; init; }

        /// <summary>Whether the shard accepts state changes.</summary>
        public required bool ExchangeActive { get; init; }

        /// <summary>Whether the shard permits trading.</summary>
        public required bool TradingActive { get; init; }

        /// <summary>Whether the shard permits intra-exchange transfers.</summary>
        public required bool IntraExchangeTransfersActive { get; init; }

        /// <summary>Human-readable shard description.</summary>
        public string? Description { get; init; }
    }
}
