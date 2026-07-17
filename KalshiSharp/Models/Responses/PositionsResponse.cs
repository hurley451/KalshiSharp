using KalshiSharp.Models.Common;
using System.Text.Json.Serialization;

namespace KalshiSharp.Models.Responses;

/// <summary>
/// Response for listing positions.
/// </summary>
public sealed record PositionsResponse : PagedResponse<PositionsResponse.MarketPosition>
{
    /// <inheritdoc />
    public override IReadOnlyList<MarketPosition> Items => MarketPositions;

    /// <summary>
    /// List of market positions.
    /// </summary>
    public required IReadOnlyList<MarketPosition> MarketPositions { get; init; }

    /// <summary>
    /// List of event positions.
    /// </summary>
    public required IReadOnlyCollection<EventPosition> EventPositions { get; init; }

    /// <summary>
    /// Represents a position held in a specific market.
    /// </summary>
    public record MarketPosition
    {
        /// <summary>
        /// Unique identifier for the market.
        /// </summary>
        public required string Ticker { get; init; }

        /*
        /// <summary>
        /// Total spent on this market in cents.
        /// </summary>
        public required int TotalTraded { get; init; }
        */

        /// <summary>
        /// Total spent on this market in dollars.
        /// </summary>
        public required string TotalTradedDollars { get; init; }

        /*
        /// <summary>
        /// Number of contracts bought in this market. Negative means NO contracts and positive means YES contracts.
        /// </summary>
        public required int Position { get; init; }
        */

        /// <summary>
        /// String representation of the number of contracts bought in this market. Negative means NO contracts and positive means YES contracts.
        /// </summary>
        public required string PositionFp { get; init; }

        /*
        /// <summary>
        /// Cost of the aggregate market position in cents.
        /// </summary>
        public required int MarketExposure { get; init; }
        */

        /// <summary>
        /// Cost of the aggregate market position in dollars.
        /// </summary>
        public required string MarketExposureDollars { get; init; }

        /*
        /// <summary>
        /// Locked in profit and loss, in cents.
        /// </summary>
        public required int RealizedPnl { get; init; }
        */

        /// <summary>
        /// Locked in profit and loss, in dollars.
        /// </summary>
        public required string RealizedPnlDollars { get; init; }

        /// <summary>
        /// [DEPRECATED] Aggregate size of resting orders in contract units.
        /// </summary>
        public int? RestingOrdersCount { get; init; }

        /*
        /// <summary>
        /// Fees paid on fill orders, in cents.
        /// </summary>
        public required int FeesPaid { get; init; }
        */

        /// <summary>
        /// Fees paid on fill orders, in dollars.
        /// </summary>
        public required string FeesPaidDollars { get; init; }

        /// <summary>
        /// Last time the position is updated.
        /// </summary>
        [JsonPropertyName("last_updated_ts")]
        public required DateTimeOffset LastUpdated { get; init; }
    }

    /// <summary>
    /// Represents a position held across all markets in an event.
    /// </summary>
    public record EventPosition
    {
        /// <summary>
        /// Unique identifier for events.
        /// </summary>
        public required string EventTicker { get; init; }

        /*
        /// <summary>
        /// Total spent on this event in cents.
        /// </summary>
        public required int TotalCost { get; init; }
        */

        /// <summary>
        /// Total spent on this event in dollars.
        /// </summary>
        public required string TotalCostDollars { get; init; }

        /*
        /// <summary>
        /// Total number of shares traded on this event (including both YES and NO contracts).
        /// </summary>
        public required int TotalCostShares { get; init; }
        */

        /// <summary>
        /// String representation of the total number of shares traded on this event (including both YES and NO contracts).
        /// </summary>
        public required string TotalCostSharesFp { get; init; }

        /*
        /// <summary>
        /// Cost of the aggregate event position in cents.
        /// </summary>
        public required int EventExposure { get; init; }
        */

        /// <summary>
        /// Cost of the aggregate event position in dollars.
        /// </summary>
        public required string EventExposureDollars { get; init; }

        /*
        /// <summary>
        /// Locked in profit and loss, in cents.
        /// </summary>
        public required int RealizedPnl { get; init; }
        */

        /// <summary>
        /// Locked in profit and loss, in dollars.
        /// </summary>
        public required string RealizedPnlDollars { get; init; }

        /*
        /// <summary>
        /// Fees paid on fill orders, in cents.
        /// </summary>
        public required int FeesPaid { get; init; }
        */

        /// <summary>
        /// Fees paid on fill orders, in dollars.
        /// </summary>
        public required string FeesPaidDollars { get; init; }
    }
}