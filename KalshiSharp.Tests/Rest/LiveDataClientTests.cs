using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using KalshiSharp.Rest.LiveData;
using KalshiSharp.Serialization;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class LiveDataClientTests
{
    [Fact]
    public async Task GetLiveDataAsync_UsesCurrentMilestoneRouteAndPlayerStatsQuery()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = CreateLiveDataResponse()
        };
        var client = new LiveDataClient(httpClient);

        var result = await client.GetLiveDataAsync("milestone/1", new LiveDataQuery
        {
            IncludePlayerStats = true
        });

        httpClient.LastRequest!.Path.Should().Be(
            "/trade-api/v2/live_data/milestone/milestone%2F1?include_player_stats=true");
        result.LiveData.Type.Should().Be("basketball");
        result.LiveData.Details.GetProperty("score").GetInt32().Should().Be(101);
    }

    [Fact]
    public async Task GetLiveDataAsync_UsesLegacyTypedRoute()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = CreateLiveDataResponse()
        };
        var client = new LiveDataClient(httpClient);

        await client.GetLiveDataAsync("sports/game", "milestone/1", new LiveDataQuery
        {
            IncludePlayerStats = false
        });

        httpClient.LastRequest!.Path.Should().Be(
            "/trade-api/v2/live_data/sports%2Fgame/milestone/milestone%2F1?include_player_stats=false");
    }

    [Fact]
    public async Task GetLiveDataBatchAsync_ParsesFlexiblePayloads()
    {
        var details = JsonSerializer.Deserialize<JsonElement>("""{"price":"1.2345","provider":"pyth"}""");
        var httpClient = new RecordingHttpClient
        {
            Response = new LiveDataBatchResponse
            {
                LiveDatas =
                [
                    new LiveDataPayload
                    {
                        Type = "pyth",
                        Details = details,
                        MilestoneId = "milestone-1"
                    }
                ]
            }
        };
        var client = new LiveDataClient(httpClient);

        var result = await client.GetLiveDataBatchAsync();

        httpClient.LastRequest!.Path.Should().Be("/trade-api/v2/live_data/batch");
        result.LiveDatas.Should().ContainSingle().Which.Details.GetProperty("provider").GetString().Should().Be("pyth");
    }

    [Fact]
    public async Task GetEventLiveDataAsync_PreservesEventMetadataAndDetails()
    {
        var details = JsonSerializer.Deserialize<JsonElement>("""{"series":[{"t":1,"v":"0.5600"}]}""");
        var httpClient = new RecordingHttpClient
        {
            Response = new EventLiveDataResponse
            {
                LiveData = new EventLiveDataPayload
                {
                    Type = "crypto_price",
                    Details = details,
                    IsHistorical = true,
                    DefaultRange = "1d",
                    RangeOptions = ["1h", "1d"]
                }
            }
        };
        var client = new LiveDataClient(httpClient);

        var result = await client.GetEventLiveDataAsync("EVENT/1");

        httpClient.LastRequest!.Path.Should().Be("/trade-api/v2/live_data/events/EVENT%2F1");
        result.LiveData.IsHistorical.Should().BeTrue();
        result.LiveData.RangeOptions.Should().Contain("1d");
        result.LiveData.Details.GetProperty("series")[0].GetProperty("v").GetString().Should().Be("0.5600");
    }

    [Fact]
    public async Task GetGameStatsAsync_UsesGameStatsRoute()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = JsonSerializer.Deserialize<GameStatsResponse>(
                """
                {
                  "pbp": {
                    "periods": [
                      { "events": [ { "clock": "12:00" } ] }
                    ]
                  }
                }
                """,
                KalshiJsonOptions.Default)!
        };
        var client = new LiveDataClient(httpClient);

        var result = await client.GetGameStatsAsync("milestone/1");

        httpClient.LastRequest!.Path.Should().Be("/trade-api/v2/live_data/milestone/milestone%2F1/game_stats");
        result.Pbp.GetProperty("periods")[0].GetProperty("events")[0].GetProperty("clock").GetString().Should().Be("12:00");
    }

    [Fact]
    public async Task GetWeatherIndexAsync_EmitsWindowAndDetailedQuery()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = CreateWeatherIndexResponse()
        };
        var client = new LiveDataClient(httpClient);

        var result = await client.GetWeatherIndexAsync("miami/beach", new WeatherIndexQuery
        {
            From = 1788768000000,
            To = 1788771600000,
            Detailed = true
        });

        httpClient.LastRequest!.Path.Should().Be(
            "/trade-api/v2/live_data/weather/miami%2Fbeach?from=1788768000000&to=1788771600000&detailed=true");
        result.City.Should().Be("miami");
    }

    [Fact]
    public async Task GetWeatherIndexAsync_DeserializesReceiptBasisAndIncompletePoint()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = JsonSerializer.Deserialize<WeatherIndexResponse>(
                """
                {
                  "city": "miami",
                  "units": "fahrenheit",
                  "timeseries": [
                    {
                      "t": 1788768000000,
                      "status": "complete",
                      "v": 84.12,
                      "contributors": 4,
                      "receipt_basis": "synoptic_latency",
                      "stations": [
                        {
                          "station_id": "KMIA",
                          "code": "KMIA",
                          "source": "nws",
                          "temp_f": 84.2,
                          "obs_time_ms": 1788767999000,
                          "received_at_ms": 1788768005000,
                          "primary_code": "KMIA"
                        }
                      ]
                    },
                    {
                      "t": 1788768060000,
                      "status": "incomplete",
                      "contributors": 1,
                      "stations": [
                        {
                          "code": "pending",
                          "temp_f": 84.4
                        }
                      ]
                    }
                  ],
                  "config_version": "miami-temperature-v1.0",
                  "future_root": true
                }
                """,
                KalshiJsonOptions.Default)!
        };
        var client = new LiveDataClient(httpClient);

        var result = await client.GetWeatherIndexAsync("miami");

        result.Units.Should().Be("fahrenheit");
        result.ConfigVersion.Should().Be("miami-temperature-v1.0");
        result.AdditionalProperties!["future_root"].GetBoolean().Should().BeTrue();

        var complete = result.Timeseries[0];
        complete.V.Should().Be(84.12m);
        complete.ReceiptBasis.Should().Be("synoptic_latency");
        complete.Stations.Should().ContainSingle().Which.ReceivedAtMs.Should().Be(1788768005000);

        var incomplete = result.Timeseries[1];
        incomplete.V.Should().BeNull();
        incomplete.ReceiptBasis.Should().BeNull();
        incomplete.Stations.Should().ContainSingle().Which.Code.Should().Be("pending");
    }

    [Fact]
    public async Task GetWeatherIndexCalibrationsAsync_DeserializesTimeline()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = JsonSerializer.Deserialize<WeatherIndexCalibrationsResponse>(
                """
                {
                  "city": "miami",
                  "units": "celsius",
                  "calibrations": [
                    {
                      "config_version": "miami-temperature-v1.1",
                      "effective_at_ms": 1788768000000,
                      "city_reference_c": 29.31,
                      "stations": [
                        {
                          "station_id": "KMIA",
                          "weight": 0.7,
                          "offset_c": -0.12,
                          "update_note": null
                        }
                      ],
                      "published_at_ms": 1788768600000,
                      "change_reason": "weekly_offset",
                      "calibration_window_start_ms": 1788163200000,
                      "calibration_window_end_ms": 1788767999999
                    }
                  ]
                }
                """,
                KalshiJsonOptions.Default)!
        };
        var client = new LiveDataClient(httpClient);

        var result = await client.GetWeatherIndexCalibrationsAsync("miami");

        httpClient.LastRequest!.Path.Should().Be("/trade-api/v2/live_data/weather/miami/calibrations");
        result.Units.Should().Be("celsius");
        var calibration = result.Calibrations.Should().ContainSingle().Which;
        calibration.CityReferenceC.Should().Be(29.31m);
        calibration.Stations.Should().ContainSingle().Which.OffsetC.Should().Be(-0.12m);
        calibration.CalibrationWindowEndMs.Should().Be(1788767999999);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WeatherEndpoints_RejectMissingCity(string city)
    {
        var client = new LiveDataClient(new RecordingHttpClient { Response = CreateWeatherIndexResponse() });

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetWeatherIndexAsync(city));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetWeatherIndexCalibrationsAsync(city));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MilestoneEndpoints_RejectMissingIdentifiers(string value)
    {
        var client = new LiveDataClient(new RecordingHttpClient { Response = CreateLiveDataResponse() });

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetLiveDataAsync(value));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetLiveDataAsync("type", value));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetLiveDataAsync(value, "milestone"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetEventLiveDataAsync(value));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetGameStatsAsync(value));
    }

    [Fact]
    public void WeatherIndexQuery_AllowsTrailingWindowAndFalseDetail()
    {
        var query = new WeatherIndexQuery { LastSec = 3600, Detailed = false };

        query.ToQueryString().Should().Be("?last_sec=3600&detailed=false");
    }

    [Fact]
    public void WeatherIndexQuery_RejectsConflictingWindows()
    {
        new WeatherIndexQuery { From = 1, To = 2, LastSec = 60 }
            .Invoking(query => query.ToQueryString())
            .Should().Throw<ArgumentException>();

        new WeatherIndexQuery { From = 1 }
            .Invoking(query => query.ToQueryString())
            .Should().Throw<ArgumentException>();

        new WeatherIndexQuery { LastSec = 0 }
            .Invoking(query => query.ToQueryString())
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("live_data")]
    public void LiveDataResponse_RejectsMissingRequiredEnvelopeMember(string memberName)
    {
        var json = JsonNode.Parse(
            """
            {
              "live_data": {
                "type": "basketball",
                "details": {},
                "milestone_id": "milestone-1"
              }
            }
            """
        )!.AsObject();
        json.Remove(memberName);

        Action act = () => JsonSerializer.Deserialize<LiveDataResponse>(
            json.ToJsonString(),
            KalshiJsonOptions.Default);

        act.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData("type")]
    [InlineData("details")]
    [InlineData("milestone_id")]
    public void LiveDataPayload_RejectsMissingRequiredMembers(string memberName)
    {
        var json = JsonNode.Parse(
            """
            {
              "live_data": {
                "type": "basketball",
                "details": {},
                "milestone_id": "milestone-1"
              }
            }
            """
        )!.AsObject();
        json["live_data"]!.AsObject().Remove(memberName);

        Action act = () => JsonSerializer.Deserialize<LiveDataResponse>(
            json.ToJsonString(),
            KalshiJsonOptions.Default);

        act.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData("city")]
    [InlineData("units")]
    [InlineData("timeseries")]
    [InlineData("config_version")]
    public void WeatherIndexResponse_RejectsMissingRequiredEnvelopeMember(string memberName)
    {
        var json = JsonNode.Parse(
            """
            {
              "city": "miami",
              "units": "fahrenheit",
              "timeseries": [],
              "config_version": ""
            }
            """
        )!.AsObject();
        json.Remove(memberName);

        Action act = () => JsonSerializer.Deserialize<WeatherIndexResponse>(
            json.ToJsonString(),
            KalshiJsonOptions.Default);

        act.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData("city")]
    [InlineData("units")]
    [InlineData("calibrations")]
    public void WeatherIndexCalibrationsResponse_RejectsMissingRequiredEnvelopeMember(string memberName)
    {
        var json = JsonNode.Parse(
            """
            {
              "city": "miami",
              "units": "celsius",
              "calibrations": []
            }
            """
        )!.AsObject();
        json.Remove(memberName);

        Action act = () => JsonSerializer.Deserialize<WeatherIndexCalibrationsResponse>(
            json.ToJsonString(),
            KalshiJsonOptions.Default);

        act.Should().Throw<JsonException>();
    }

    private static WeatherIndexResponse CreateWeatherIndexResponse() => new()
    {
        City = "miami",
        Units = "fahrenheit",
        Timeseries = [],
        ConfigVersion = string.Empty
    };

    private static LiveDataResponse CreateLiveDataResponse() => new()
    {
        LiveData = new LiveDataPayload
        {
            Type = "basketball",
            Details = JsonSerializer.Deserialize<JsonElement>("""{"score":101}"""),
            MilestoneId = "milestone-1"
        }
    };

    private sealed class RecordingHttpClient : IKalshiHttpClient
    {
        public required object Response { get; init; }

        public KalshiRequest? LastRequest { get; private set; }

        public Task<TResponse> SendAsync<TResponse>(
            KalshiRequest request,
            CancellationToken cancellationToken = default)
            where TResponse : class
        {
            LastRequest = request;
            return Task.FromResult((TResponse)Response);
        }

        public Task SendAsync(KalshiRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.CompletedTask;
        }
    }
}
