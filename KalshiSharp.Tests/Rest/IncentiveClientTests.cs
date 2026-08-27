using FluentAssertions;
using System.Globalization;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Errors;
using KalshiSharp.Http;
using KalshiSharp.Models.Enums;
using KalshiSharp.Models.Requests;
using KalshiSharp.Rest.Incentives;
using KalshiSharp.Tests.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class IncentiveClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly IncentiveClient _client;
    private readonly IKalshiRequestSigner _signer;

    public IncentiveClientTests()
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
        var httpClient = new HttpClient(signingHandler);
        var kalshiHttpClient = new KalshiHttpClient(
            httpClient,
            options,
            NullLogger<KalshiHttpClient>.Instance);

        _client = new IncentiveClient(kalshiHttpClient);
    }

    public void Dispose()
    {
        _server.Dispose();
        (_signer as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task ListIncentiveProgramsAsync_DeserializesCurrentPayloadAndNextCursor()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/incentive_programs")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "incentive_programs": [
                        {
                            "id": "program-1",
                            "market_id": "market-1",
                            "market_ticker": "MARKET-1",
                            "series_ticker": "SERIES-1",
                            "incentive_type": "future_type",
                            "incentive_description": "series_lip",
                            "start_date": "2026-08-27T03:45:00Z",
                            "end_date": "2026-08-27T04:00:00Z",
                            "period_reward": 200000,
                            "paid_out": false,
                            "discount_factor_bps": 5000,
                            "target_size_fp": "1000.00",
                            "unknown_future_field": "preserved compatibility"
                        }
                    ],
                    "next_cursor": "next-page"
                }
                """));

        var result = await _client.ListIncentiveProgramsAsync();

        result.Items.Should().ContainSingle();
        result.Cursor.Should().Be("next-page");
        result.NextCursor.Should().Be("next-page");
        result.HasMore.Should().BeTrue();
        var program = result.Items[0];
        program.Id.Should().Be("program-1");
        program.MarketId.Should().Be("market-1");
        program.MarketTicker.Should().Be("MARKET-1");
        program.SeriesTicker.Should().Be("SERIES-1");
        program.IncentiveType.Should().Be("future_type");
        program.IncentiveDescription.Should().Be("series_lip");
        program.StartDate.Should().Be(DateTimeOffset.Parse("2026-08-27T03:45:00Z", CultureInfo.InvariantCulture));
        program.EndDate.Should().Be(DateTimeOffset.Parse("2026-08-27T04:00:00Z", CultureInfo.InvariantCulture));
        program.PeriodReward.Should().Be(200000);
        program.PaidOut.Should().BeFalse();
        program.DiscountFactorBps.Should().Be(5000);
        program.TargetSizeFp.Should().Be("1000.00");
    }

    [Fact]
    public async Task ListIncentiveProgramsAsync_WithFilters_EncodesCurrentContract()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/incentive_programs")
                .WithParam("limit", "10000")
                .WithParam("cursor", "next/page+1")
                .WithParam("status", "paid_out")
                .WithParam("type", "liquidity")
                .WithParam("incentive_description", "series lip & volume")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"incentive_programs":[],"next_cursor":""}"""));

        var firstPage = await _client.ListIncentiveProgramsAsync(new IncentiveProgramQuery
        {
            Limit = 10_000,
            Cursor = "next/page+1",
            Status = IncentiveProgramStatus.PaidOut,
            Type = IncentiveProgramType.Liquidity,
            IncentiveDescription = "series lip & volume"
        });

        firstPage.Items.Should().BeEmpty();
        firstPage.HasMore.Should().BeFalse();
    }

    [Theory]
    [InlineData(IncentiveProgramStatus.All, "all")]
    [InlineData(IncentiveProgramStatus.Active, "active")]
    [InlineData(IncentiveProgramStatus.Upcoming, "upcoming")]
    [InlineData(IncentiveProgramStatus.Closed, "closed")]
    [InlineData(IncentiveProgramStatus.PaidOut, "paid_out")]
    public void ToQueryString_MapsEveryStatus(IncentiveProgramStatus status, string expected)
    {
        new IncentiveProgramQuery { Status = status }.ToQueryString()
            .Should().Be($"?status={expected}");
    }

    [Theory]
    [InlineData(IncentiveProgramType.All, "all")]
    [InlineData(IncentiveProgramType.Liquidity, "liquidity")]
    [InlineData(IncentiveProgramType.Volume, "volume")]
    [InlineData(IncentiveProgramType.MarginMakerVolume, "margin_maker_volume")]
    public void ToQueryString_MapsEveryType(IncentiveProgramType type, string expected)
    {
        new IncentiveProgramQuery { Type = type }.ToQueryString()
            .Should().Be($"?type={expected}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10_000)]
    public void ToQueryString_AcceptsLimitBoundaries(int limit)
    {
        new IncentiveProgramQuery { Limit = limit }.ToQueryString()
            .Should().Be($"?limit={limit}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10_001)]
    public void ToQueryString_RejectsInvalidLimits(int limit)
    {
        var act = () => new IncentiveProgramQuery { Limit = limit }.ToQueryString();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("Limit");
    }

    [Fact]
    public async Task ListIncentiveProgramsAsync_DeserializesMarginProgramWithoutEventFields()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/incentive_programs")
                .WithParam("type", "margin_maker_volume")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "incentive_programs": [
                        {
                            "id": "margin-program-1",
                            "incentive_type": "margin_maker_volume",
                            "incentive_description": "margin maker volume",
                            "start_date": "2026-08-27T04:00:00Z",
                            "end_date": "2026-08-28T04:00:00Z",
                            "period_reward": 500000,
                            "paid_out": false,
                            "max_reward_per_account": 125000
                        }
                    ]
                }
                """));

        var result = await _client.ListIncentiveProgramsAsync(new IncentiveProgramQuery
        {
            Type = IncentiveProgramType.MarginMakerVolume
        });

        var program = result.Items.Should().ContainSingle().Which;
        program.MarketId.Should().BeNull();
        program.MarketTicker.Should().BeNull();
        program.IncentiveType.Should().Be("margin_maker_volume");
        program.MaxRewardPerAccount.Should().Be(125000);
    }

    [Fact]
    public async Task ListIncentiveProgramsAsync_MissingCommonIdRemainsInvalid()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/incentive_programs")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "incentive_programs": [
                        {
                            "incentive_type": "margin_maker_volume",
                            "incentive_description": "margin maker volume",
                            "start_date": "2026-08-27T04:00:00Z",
                            "end_date": "2026-08-28T04:00:00Z",
                            "period_reward": 500000,
                            "paid_out": false
                        }
                    ]
                }
                """));

        var action = () => _client.ListIncentiveProgramsAsync();

        await action.Should().ThrowAsync<KalshiException>()
            .WithMessage("Failed to deserialize response:*");
    }
}
