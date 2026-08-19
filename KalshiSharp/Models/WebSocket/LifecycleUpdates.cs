using System.Text.Json;
using System.Text.Json.Serialization;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Models.WebSocket;

/// <summary>Lifecycle update for a standard market.</summary>
public sealed record MarketLifecycleUpdate : WebSocketMessage<MarketLifecycleMessage>
{
    /// <inheritdoc />
    public override string Type => "market_lifecycle_v2";
}

/// <summary>Lifecycle update for a multivariate market.</summary>
public sealed record MultivariateMarketLifecycleUpdate : WebSocketMessage<MarketLifecycleMessage>
{
    /// <inheritdoc />
    public override string Type => "multivariate_market_lifecycle";
}

/// <summary>Current market lifecycle payload.</summary>
public sealed record MarketLifecycleMessage
{
    /// <summary>Market ticker.</summary>
    [JsonPropertyName("market_ticker")]
    public required string MarketTicker { get; init; }

    /// <summary>Lifecycle event name.</summary>
    [JsonPropertyName("event_type")]
    public required string EventType { get; init; }

    /// <summary>Exchange shard index.</summary>
    [JsonPropertyName("exchange_index")]
    public int? ExchangeIndex { get; init; }

    /// <summary>Market open timestamp.</summary>
    [JsonPropertyName("open_ts")]
    public long? OpenTs { get; init; }

    /// <summary>Market close timestamp.</summary>
    [JsonPropertyName("close_ts")]
    public long? CloseTs { get; init; }

    /// <summary>Settlement value when determined.</summary>
    [JsonPropertyName("settlement_value")]
    public string? SettlementValue { get; init; }

    /// <summary>Current price-level structure.</summary>
    [JsonPropertyName("price_level_structure")]
    public string? PriceLevelStructure { get; init; }

    /// <summary>Current dynamic price ranges.</summary>
    [JsonPropertyName("price_ranges")]
    public IReadOnlyList<MarketResponse.PriceRange> PriceRanges { get; init; } = [];

    /// <summary>Creation or metadata-update details.</summary>
    [JsonPropertyName("additional_metadata")]
    public LifecycleMarketMetadata? AdditionalMetadata { get; init; }
}

/// <summary>Metadata supplied by lifecycle creation and metadata-update events.</summary>
public sealed record LifecycleMarketMetadata
{
    /// <summary>Display name.</summary>
    public string? Name { get; init; }

    /// <summary>Market title.</summary>
    public string? Title { get; init; }

    /// <summary>YES subtitle.</summary>
    public string? YesSubTitle { get; init; }

    /// <summary>NO subtitle.</summary>
    public string? NoSubTitle { get; init; }

    /// <summary>Primary market rules.</summary>
    public string? RulesPrimary { get; init; }

    /// <summary>Secondary market rules.</summary>
    public string? RulesSecondary { get; init; }

    /// <summary>Whether the market may close early.</summary>
    public bool? CanCloseEarly { get; init; }

    /// <summary>Parent event ticker.</summary>
    public string? EventTicker { get; init; }

    /// <summary>Expected expiration timestamp.</summary>
    public long? ExpectedExpirationTs { get; init; }

    /// <summary>Strike type.</summary>
    public string? StrikeType { get; init; }

    /// <summary>Floor strike.</summary>
    public decimal? FloorStrike { get; init; }

    /// <summary>Cap strike.</summary>
    public decimal? CapStrike { get; init; }

    /// <summary>Custom strike value whose JSON shape may vary by market type.</summary>
    public JsonElement? CustomStrike { get; init; }
}

/// <summary>Lifecycle update for an event.</summary>
public sealed record EventLifecycleUpdate : WebSocketMessage<EventLifecycleUpdate.MessageBody>
{
    /// <inheritdoc />
    public override string Type => "event_lifecycle";

    /// <summary>Current event lifecycle payload.</summary>
    public sealed record MessageBody
    {
        /// <summary>Event ticker.</summary>
        public required string EventTicker { get; init; }

        /// <summary>Exchange shard index.</summary>
        public int? ExchangeIndex { get; init; }

        /// <summary>Event title.</summary>
        public string? Title { get; init; }

        /// <summary>Event subtitle.</summary>
        public string? Subtitle { get; init; }

        /// <summary>Collateral return type.</summary>
        public string? CollateralReturnType { get; init; }

        /// <summary>Parent series ticker.</summary>
        public string? SeriesTicker { get; init; }
    }
}

/// <summary>Fee update for an event.</summary>
public sealed record EventFeeUpdate : WebSocketMessage<EventFeeUpdate.MessageBody>
{
    /// <inheritdoc />
    public override string Type => "event_fee_update";

    /// <summary>Current fee-update payload.</summary>
    public sealed record MessageBody
    {
        /// <summary>Event ticker.</summary>
        public required string EventTicker { get; init; }

        /// <summary>Fee type override.</summary>
        public string? FeeTypeOverride { get; init; }

        /// <summary>Fee multiplier override.</summary>
        public decimal? FeeMultiplierOverride { get; init; }
    }
}
