using FluentAssertions;
using System.Globalization;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Http;
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Requests;
using KalshiSharp.Rest.Historical;
using KalshiSharp.Tests.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Historical;

/// <summary>HTTP contract tests for explicit historical-data access.</summary>
public sealed class HistoricalClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly HistoricalClient _client;
    private readonly IKalshiRequestSigner _signer;

    public HistoricalClientTests()
    {
        _server = WireMockServer.Start();
        var options = Options.Create(new KalshiClientOptions
        {
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret",
            BaseUri = new Uri(_server.Url!),
            Timeout = TimeSpan.FromSeconds(5)
        });
        _signer = new MockRequestSigner(options.Value.ApiKey, options.Value.ApiSecret);
        var signingHandler = new SigningDelegatingHandler(
            _signer,
            new SystemClock(),
            NullLogger<SigningDelegatingHandler>.Instance)
        {
            InnerHandler = new HttpClientHandler()
        };
        var httpClient = new KalshiHttpClient(
            new HttpClient(signingHandler),
            options,
            NullLogger<KalshiHttpClient>.Instance);
        _client = new HistoricalClient(httpClient);
    }

    public void Dispose()
    {
        _server.Dispose();
        (_signer as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task GetCutoffAsync_ReturnsPositionCutoff()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/historical/cutoff")
                .UsingGet())
            .RespondWith(JsonResponse("""
                {
                  "market_settled_ts": "2026-05-01T00:00:00Z",
                  "trades_created_ts": "2026-05-02T00:00:00Z",
                  "orders_updated_ts": "2026-05-03T00:00:00Z",
                  "market_positions_last_updated_ts": "2026-05-04T00:00:00Z"
                }
                """));

        var result = await _client.GetCutoffAsync();

        result.MarketPositionsLastUpdatedTs.Should().Be(
            DateTimeOffset.Parse("2026-05-04T00:00:00Z", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MarketEndpoints_UseExplicitHistoricalRoutesAndCurrentMarketFields()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/historical/markets")
                .WithParam("series_ticker", "KXTEST")
                .UsingGet())
            .RespondWith(JsonResponse("""
                {
                  "markets": [{
                    "ticker": "KXTEST-26AUG19",
                    "event_ticker": "KXTEST-26AUG",
                    "title": "Archived market",
                    "status": "finalized",
                    "yes_bid_dollars": "0.4325",
                    "price_level_structure": "deci_cent",
                    "price_ranges": [{"start":"0.0000","end":"1.0000","step":"0.0010"}],
                    "exchange_index": 1
                  }],
                  "cursor": "next"
                }
                """));
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/historical/markets/KXTEST-26AUG19")
                .UsingGet())
            .RespondWith(JsonResponse("""
                {
                  "market": {
                    "ticker": "KXTEST-26AUG19",
                    "event_ticker": "KXTEST-26AUG",
                    "title": "Archived market",
                    "status": "finalized",
                    "yes_bid_dollars": "0.4325"
                  }
                }
                """));

        var markets = await _client.ListMarketsAsync(new HistoricalMarketQuery { SeriesTicker = "KXTEST" });
        var market = await _client.GetMarketAsync("KXTEST-26AUG19");

        markets.Markets.Should().ContainSingle();
        markets.Markets[0].PriceRanges.Should().ContainSingle();
        markets.Markets[0].ExchangeIndex.Should().Be(1);
        market.YesBidDollars.Should().Be("0.4325");
    }

    [Fact]
    public async Task GetMarketCandlesticksAsync_UsesArchivedResponseContract()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/historical/markets/KXTEST-26AUG19/candlesticks")
                .WithParam("start_ts", "1787097600")
                .WithParam("end_ts", "1787184000")
                .WithParam("period_interval", "60")
                .UsingGet())
            .RespondWith(JsonResponse("""
                {
                  "ticker": "KXTEST-26AUG19",
                  "candlesticks": [{
                    "end_period_ts": 1787101200,
                    "yes_bid": {"open":"0.4300","low":"0.4200","high":"0.4400","close":"0.4325"},
                    "yes_ask": {"open":"0.4400","low":"0.4300","high":"0.4500","close":"0.4425"},
                    "price": {"open":"0.4350","low":"0.4250","high":"0.4450","close":"0.4375","mean":"0.4360","previous":"0.4300"},
                    "volume": "10.50",
                    "open_interest": "25.00"
                  }]
                }
                """));
        var query = new MarketCandlesticksQuery
        {
            StartTimestamp = DateTimeOffset.FromUnixTimeSeconds(1787097600),
            EndTimestamp = DateTimeOffset.FromUnixTimeSeconds(1787184000),
            PeriodInterval = PeriodInterval.OneHour
        };

        var result = await _client.GetMarketCandlesticksAsync("KXTEST-26AUG19", query);

        result.Candlesticks.Should().ContainSingle();
        result.Candlesticks[0].YesBid.Close.Should().Be("0.4325");
        result.Candlesticks[0].Volume.Should().Be("10.50");
    }

    [Fact]
    public async Task GetMarketCandlesticksAsync_RejectsActiveOnlyLatestFlag()
    {
        var query = new MarketCandlesticksQuery
        {
            StartTimestamp = DateTimeOffset.FromUnixTimeSeconds(1787097600),
            EndTimestamp = DateTimeOffset.FromUnixTimeSeconds(1787184000),
            PeriodInterval = PeriodInterval.OneHour,
            IncludeLatestBeforeStart = true
        };

        var act = () => _client.GetMarketCandlesticksAsync("KXTEST-26AUG19", query);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ArchiveCollections_DeserializeActiveContracts()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/historical/trades")
                .WithParam("ticker", "KXTEST-26AUG19")
                .WithParam("is_block_trade", "true")
                .UsingGet())
            .RespondWith(JsonResponse("""
                {"trades":[{"trade_id":"trade-1","ticker":"KXTEST-26AUG19","count_fp":"2.50","yes_price_dollars":"0.4325","no_price_dollars":"0.5675","created_time":"2026-05-01T00:00:00Z","is_block_trade":true}],"cursor":""}
                """));
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/historical/fills")
                .WithParam("ticker", "KXTEST-26AUG19")
                .UsingGet())
            .RespondWith(JsonResponse("""
                {"fills":[{"fill_id":"fill-1","exchange_index":1,"trade_id":"trade-1","order_id":"order-1","ticker":"KXTEST-26AUG19","count_fp":"2.50","yes_price_dollars":"0.4325","is_taker":true,"created_time":"2026-05-01T00:00:00Z","subaccount_number":2}],"cursor":""}
                """));
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/historical/orders")
                .WithParam("ticker", "KXTEST-26AUG19")
                .UsingGet())
            .RespondWith(JsonResponse("""
                {"orders":[{"order_id":"order-1","ticker":"KXTEST-26AUG19","yes_price_dollars":"0.4325","fill_count_fp":"2.50","exchange_index":1}],"cursor":""}
                """));
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/historical/positions")
                .WithParam("event_ticker", "KXTEST-26AUG")
                .UsingGet())
            .RespondWith(JsonResponse("""
                {"market_positions":[{"ticker":"KXTEST-26AUG19","exchange_index":1,"position_fp":"2.50","market_exposure_dollars":"1.08125","last_updated_ts":"2026-05-01T00:00:00Z"}],"event_positions":[],"cursor":""}
                """));

        var trades = await _client.ListTradesAsync(new HistoricalTradeQuery { Ticker = "KXTEST-26AUG19", IsBlockTrade = true });
        var fills = await _client.ListFillsAsync(new HistoricalFillQuery { Ticker = "KXTEST-26AUG19" });
        var orders = await _client.ListOrdersAsync(new HistoricalOrderQuery { Ticker = "KXTEST-26AUG19" });
        var positions = await _client.ListPositionsAsync(new HistoricalPositionQuery { EventTicker = "KXTEST-26AUG" });

        trades.Trades[0].IsBlockTrade.Should().BeTrue();
        fills.Fills[0].SubaccountNumber.Should().Be(2);
        fills.Fills[0].ExchangeIndex.Should().Be(1);
        orders.Orders[0].FillCountFp.Should().Be("2.50");
        positions.MarketPositions[0].ExchangeIndex.Should().Be(1);
    }

    private static IResponseBuilder JsonResponse(string body) => Response.Create()
        .WithStatusCode(200)
        .WithHeader("Content-Type", "application/json")
        .WithBody(body);
}
