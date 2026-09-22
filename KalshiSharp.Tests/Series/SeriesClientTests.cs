using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using KalshiSharp.Rest.Series;
using KalshiSharp.Serialization;
using Xunit;

namespace KalshiSharp.Tests.Series;

public sealed class SeriesClientTests
{
    [Fact]
    public async Task GetSeriesAsync_ReturnsCurrentWireShape()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = JsonSerializer.Deserialize<SingleSeriesResponse>(
                """
                {
                  "series": {
                    "ticker": "KXHIGHNY",
                    "frequency": "daily",
                    "title": "New York high temperature",
                    "category": "Climate and Weather",
                    "categories": ["Climate and Weather", "Commodities"],
                    "tags": ["Daily temperature"],
                    "settlement_sources": [{"name":"NWS","url":"https://example.com/source"}],
                    "contract_url": "https://example.com/contract",
                    "contract_terms_url": "https://example.com/terms",
                    "fee_type": "future_fee_type",
                    "fee_multiplier": 0.25,
                    "additional_prohibitions": ["Restricted jurisdiction"],
                    "product_metadata": {"publication":{"cadence":"daily"}},
                    "volume_fp": "1234.50",
                    "last_updated_ts": "2026-08-26T12:34:56.789Z",
                    "exchange_index": 0,
                    "future_field": true
                  }
                }
                """,
                KalshiJsonOptions.Default)!
        };
        var client = new SeriesClient(httpClient);

        var result = await client.GetSeriesAsync("KXHIGHNY", includeVolume: true);

        httpClient.LastRequest!.Path.Should().Be("/trade-api/v2/series/KXHIGHNY?include_volume=true");
        result.Ticker.Should().Be("KXHIGHNY");
        result.Category.Should().Be("Climate and Weather");
        result.Categories.Should().Equal("Climate and Weather", "Commodities");
        result.FeeType.Should().Be("future_fee_type");
        result.FeeMultiplier.Should().Be(0.25m);
        result.Tags.Should().Equal("Daily temperature");
        result.SettlementSources.Should().ContainSingle().Which.Name.Should().Be("NWS");
        result.ProductMetadata!.Value.GetProperty("publication").GetProperty("cadence").GetString()
            .Should().Be("daily");
        result.VolumeFp.Should().Be("1234.50");
        result.LastUpdatedTs.Should().Be(
            DateTimeOffset.Parse("2026-08-26T12:34:56.789Z", CultureInfo.InvariantCulture));
        result.ExchangeIndex.Should().Be(0);
    }

    [Fact]
    public async Task GetSeriesAsync_EscapesTickerAndIncludesFalseVolume()
    {
        var httpClient = new RecordingHttpClient { Response = CreateSingleResponse() };
        var client = new SeriesClient(httpClient);

        await client.GetSeriesAsync("SERIES/ONE", includeVolume: false);

        httpClient.LastRequest!.Path.Should()
            .Be("/trade-api/v2/series/SERIES%2FONE?include_volume=false");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSeriesAsync_RejectsMissingTicker(string ticker)
    {
        var client = new SeriesClient(new RecordingHttpClient { Response = CreateSingleResponse() });

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetSeriesAsync(ticker));
    }

    [Fact]
    public async Task ListSeriesAsync_IncludesSupportedFilters()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new SeriesListResponse { Series = [] }
        };
        var client = new SeriesClient(httpClient);
        var minUpdated = DateTimeOffset.FromUnixTimeSeconds(1787745600);

        var result = await client.ListSeriesAsync(new SeriesQuery
        {
            Category = "Climate and Weather",
            Tags = ["Daily temperature", "New York"],
            IncludeProductMetadata = false,
            IncludeVolume = true,
            MinUpdatedTs = minUpdated
        });

        httpClient.LastRequest!.Path.Should().Be(
            "/trade-api/v2/series?category=Climate%20and%20Weather" +
            "&tags=Daily%20temperature%2CNew%20York" +
            "&include_product_metadata=false" +
            "&include_volume=true" +
            "&min_updated_ts=1787745600");
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListSeriesAsync_AllowsNullableCollectionsAndPartialSources()
    {
        var response = JsonSerializer.Deserialize<SeriesListResponse>(
            """
            {
              "series": [
                {
                  "ticker": "SERIES-1",
                  "frequency": "monthly",
                  "title": "Series",
                  "category": "Other",
                  "tags": null,
                  "settlement_sources": [{"name":"Named source"}, {"url":"https://example.com"}],
                  "contract_url": "https://example.com/contract",
                  "contract_terms_url": "https://example.com/terms",
                  "fee_type": "quadratic",
                  "fee_multiplier": 1,
                  "additional_prohibitions": null
                }
              ]
            }
            """,
            KalshiJsonOptions.Default)!;
        var httpClient = new RecordingHttpClient { Response = response };
        var client = new SeriesClient(httpClient);

        var result = await client.ListSeriesAsync();

        var series = result.Items.Should().ContainSingle().Which;
        series.Tags.Should().BeNull();
        series.AdditionalProhibitions.Should().BeNull();
        series.SettlementSources![0].Name.Should().Be("Named source");
        series.SettlementSources[0].Url.Should().BeNull();
        series.SettlementSources[1].Name.Should().BeNull();
        series.SettlementSources[1].Url.Should().Be("https://example.com");
    }

    [Fact]
    public void SeriesResponse_AllowsMissingDiscoveryCategoriesForCapturedPayloads()
    {
        var result = JsonSerializer.Deserialize<SeriesResponse>(
            """
            {
              "ticker": "SERIES-1",
              "frequency": "daily",
              "title": "Series",
              "category": "Other",
              "tags": null,
              "settlement_sources": null,
              "contract_url": "https://example.com/contract",
              "contract_terms_url": "https://example.com/terms",
              "fee_type": "quadratic",
              "fee_multiplier": 1,
              "additional_prohibitions": null
            }
            """,
            KalshiJsonOptions.Default);

        result!.Categories.Should().BeNull();
    }

    [Theory]
    [InlineData("tags")]
    [InlineData("settlement_sources")]
    [InlineData("additional_prohibitions")]
    public void SeriesResponse_RejectsMissingRequiredCollectionMember(string memberName)
    {
        var json = JsonNode.Parse(
            """
            {
              "ticker": "SERIES-1",
              "frequency": "daily",
              "title": "Series",
              "category": "Other",
              "tags": null,
              "settlement_sources": null,
              "contract_url": "https://example.com/contract",
              "contract_terms_url": "https://example.com/terms",
              "fee_type": "quadratic",
              "fee_multiplier": 1,
              "additional_prohibitions": null
            }
            """
        )!.AsObject();
        json.Remove(memberName);

        Action act = () => JsonSerializer.Deserialize<SeriesResponse>(
            json.ToJsonString(),
            KalshiJsonOptions.Default);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void SeriesListResponse_RejectsMissingRequiredSeriesMember()
    {
        Action act = () => JsonSerializer.Deserialize<SeriesListResponse>(
            "{}",
            KalshiJsonOptions.Default);

        act.Should().Throw<JsonException>();
    }

    private static SingleSeriesResponse CreateSingleResponse() => new()
    {
        Series = new SeriesResponse
        {
            Ticker = "SERIES-1",
            Frequency = "daily",
            Title = "Series",
            Category = "Other",
            Tags = null,
            SettlementSources = null,
            ContractUrl = "https://example.com/contract",
            ContractTermsUrl = "https://example.com/terms",
            FeeType = "quadratic",
            FeeMultiplier = 1,
            AdditionalProhibitions = []
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
