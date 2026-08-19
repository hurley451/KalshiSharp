using System.Text.Json.Serialization;
using KalshiSharp.Models.Enums;

namespace KalshiSharp.Models.WebSocket;

/// <summary>Current private user-order update.</summary>
public sealed record UserOrderUpdate : WebSocketMessage<UserOrderUpdate.MessageBody>
{
    /// <inheritdoc/>
    public override string Type => "user_order";

    /// <summary>Current user-order payload.</summary>
    public sealed record MessageBody
    {
        /// <summary>Order identifier.</summary>
        public required string OrderId { get; init; }

        /// <summary>User identifier.</summary>
        public string? UserId { get; init; }

        /// <summary>Market ticker.</summary>
        public required string Ticker { get; init; }

        /// <summary>Order status.</summary>
        public OrderStatus? Status { get; init; }

        /// <summary>Legacy outcome side.</summary>
        public OrderSide? Side { get; init; }

        /// <summary>Canonical outcome side.</summary>
        public OrderSide? OutcomeSide { get; init; }

        /// <summary>Canonical book side.</summary>
        public OrderBookSide? BookSide { get; init; }

        /// <summary>YES price in dollars.</summary>
        public string? YesPriceDollars { get; init; }

        /// <summary>Filled quantity as a fixed-point count.</summary>
        public string? FillCountFp { get; init; }

        /// <summary>Remaining quantity as a fixed-point count.</summary>
        public string? RemainingCountFp { get; init; }

        /// <summary>Initial quantity as a fixed-point count.</summary>
        public string? InitialCountFp { get; init; }

        /// <summary>Client-provided order identifier.</summary>
        public string? ClientOrderId { get; init; }

        /// <summary>Order group identifier.</summary>
        public string? OrderGroupId { get; init; }

        /// <summary>Self-trade prevention strategy.</summary>
        public string? SelfTradePreventionType { get; init; }

        /// <summary>Legacy creation time.</summary>
        public DateTimeOffset? CreatedTime { get; init; }

        /// <summary>Creation time in Unix milliseconds.</summary>
        public long? CreatedTsMs { get; init; }

        /// <summary>Legacy last-update time.</summary>
        public DateTimeOffset? LastUpdateTime { get; init; }

        /// <summary>Last-update time in Unix milliseconds.</summary>
        public long? LastUpdatedTsMs { get; init; }

        /// <summary>Legacy expiration time.</summary>
        public DateTimeOffset? ExpirationTime { get; init; }

        /// <summary>Expiration time in Unix milliseconds.</summary>
        public long? ExpirationTsMs { get; init; }

        /// <summary>Subaccount number.</summary>
        public int? SubaccountNumber { get; init; }
    }
}
