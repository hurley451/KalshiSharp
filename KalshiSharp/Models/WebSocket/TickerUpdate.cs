using System.Text.Json.Serialization;

namespace KalshiSharp.Models.WebSocket
{
    public sealed record TickerUpdate : WebSocketMessage<TickerUpdate.MessageBody>
    {
        /// <inheritdoc/>        
        public override string Type => "ticker";

        public sealed record MessageBody
        {
            /// <summary>
            /// Unique market identifier
            /// </summary>
            [JsonPropertyName("market_ticker")]
            public required string MarketTicker { get; init; }

            /// <summary>
            /// Unique market UUID
            /// </summary>
            [JsonPropertyName("market_id")]
            public Guid MarketId { get; init; }

            /// <summary>
            /// Last traded price in cents (1-99)
            /// </summary>
            [JsonPropertyName("price")]
            public int? Price { get; init; }

            /// <summary>
            /// Best bid price for yes side
            /// </summary>
            [JsonPropertyName("yes_bid")]
            public int? YesBid { get; init; }

            /// <summary>
            /// Best ask price for yes side
            /// </summary>
            [JsonPropertyName("yes_ask")]
            public int? YesAsk { get; init; }

            /// <summary>
            /// Last traded price in dollars
            /// </summary>
            [JsonPropertyName("price_dollars")]
            public string? PriceDollars { get; init; }

            /// <summary>
            /// Best ask price for yes side
            /// </summary>
            [JsonPropertyName("yes_bid_dollars")]
            public string? YesBidDollars { get; init; }

            /// <summary>
            /// Best bid price for yes side in dollars
            /// </summary>
            [JsonPropertyName("yes_ask_dollars")]
            public string? YesAskDollars { get; init; }

            /// <summary>
            /// Number of individual contracts traded on the market so far. YES and NO count separately
            /// </summary>
            [JsonPropertyName("volume")]
            public int? Volume { get; init; }

            /// <summary>
            /// Number of individual contracts traded on the market so far - Fixed-point. YES and NO count separately
            /// </summary>
            [JsonPropertyName("volume_fp")]
            public decimal? VolumeFp { get; init; }

            /// <summary>
            /// Number of active contracts in the market currently
            /// </summary>
            [JsonPropertyName("open_interest")]
            public int? OpenInterest { get; init; }

            /// <summary>
            /// Number of active contracts in the market currently - Fixed-point
            /// </summary>
            [JsonPropertyName("open_interest_fp")]
            public decimal? OpenInterestFp { get; init; }

            /// <summary>
            /// Unix timestamp for when the update happened (in seconds)
            /// </summary>
            [JsonPropertyName("ts")]
            public long TimeStamp { get; init; }

            /// <summary>
            /// Number of dollars traded in the market so far
            /// </summary>
            [JsonPropertyName("dollar_volume")]
            public int? DollarVolume { get; init; }

            /// <summary>
            /// Number of dollars positioned in the market currently
            /// </summary>
            [JsonPropertyName("dollar_open_interest")]
            public int? DollarOpenInterest { get; init; }

            /// <summary>
            /// Timestamp for when the update happened
            /// </summary>
            [JsonPropertyName("time")]
            public DateTimeOffset Time { get; init; }

            /// <summary>
            /// Calculated best bid price for no side
            /// </summary>
            [JsonIgnore]
            public int? NoBid { get => 100 - YesAsk; }

            /// <summary>
            /// Calculated best ask price for no side.
            /// </summary>
            [JsonIgnore]
            public int? NoAsk { get => 100 - YesBid;}
        }
    }
}
