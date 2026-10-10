using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using KalshiSharp.Rest.Multivariate;
using KalshiSharp.Serialization;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class MultivariateClientTests
{
    [Fact]
    public async Task ListEventCollectionsAsync_EmitsDocumentedFiltersAndParsesPriceGrid()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = JsonSerializer.Deserialize<MultivariateEventCollectionsResponse>(
                """
                {
                  "multivariate_contracts": [
                    {
                      "collection_ticker": "KXCOMBO",
                      "series_ticker": "KXSERIES",
                      "exchange_index": 2,
                      "title": "Combo",
                      "description": "Example combo collection",
                      "open_date": "2026-10-08T13:00:00Z",
                      "close_date": "2026-10-31T20:00:00Z",
                      "associated_events": [
                        {
                          "ticker": "EVENT-1",
                          "is_yes_only": true,
                          "size_min": 1,
                          "size_max": null,
                          "active_quoters": ["comm-1"]
                        }
                      ],
                      "associated_event_tickers": ["EVENT-1"],
                      "is_ordered": false,
                      "is_single_market_per_event": true,
                      "is_all_yes": true,
                      "size_min": 2,
                      "size_max": 4,
                      "functional_description": "selected markets form the output",
                      "price_level_structure": "center_half_edge_quint_cent",
                      "price_ranges": [
                        {"start": "0.0000", "end": "0.1000", "step": "0.0020"}
                      ]
                    }
                  ],
                  "cursor": "next page"
                }
                """,
                KalshiJsonOptions.Default)!
        };
        var client = new MultivariateClient(httpClient);

        var result = await client.ListEventCollectionsAsync(new MultivariateEventCollectionQuery
        {
            Limit = 200,
            Cursor = "next page",
            Status = "open",
            AssociatedEventTicker = "EVENT/1",
            SeriesTicker = "KXSERIES"
        });

        httpClient.LastRequest!.Path.Should().Be(
            "/trade-api/v2/multivariate_event_collections?limit=200&cursor=next%20page" +
            "&status=open&associated_event_ticker=EVENT%2F1&series_ticker=KXSERIES");
        result.HasMore.Should().BeTrue();
        var collection = result.Items.Should().ContainSingle().Subject;
        collection.CollectionTicker.Should().Be("KXCOMBO");
        collection.OpenDate.Should().Be(
            DateTimeOffset.Parse("2026-10-08T13:00:00Z", CultureInfo.InvariantCulture));
        collection.AssociatedEvents.Should().ContainSingle().Which.ActiveQuoters.Should().ContainSingle("comm-1");
        collection.PriceLevelStructure.Should().Be("center_half_edge_quint_cent");
        collection.PriceRanges.Should().ContainSingle().Which.Step.Should().Be("0.0020");
    }

    [Fact]
    public async Task GetEventCollectionAsync_EscapesTickerAndAllowsOmittedWrapperMember()
    {
        var httpClient = new RecordingHttpClient { Response = new SingleMultivariateEventCollectionResponse() };
        var client = new MultivariateClient(httpClient);

        var result = await client.GetEventCollectionAsync("KX/COMBO");

        httpClient.LastRequest!.Path.Should().Be("/trade-api/v2/multivariate_event_collections/KX%2FCOMBO");
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetEventCollectionAsync_RejectsMissingTicker(string ticker)
    {
        var client = new MultivariateClient(new RecordingHttpClient
        {
            Response = new SingleMultivariateEventCollectionResponse()
        });

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetEventCollectionAsync(ticker));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void MultivariateEventCollectionQuery_RejectsInvalidLimit(int limit)
    {
        var query = new MultivariateEventCollectionQuery { Limit = limit };

        var action = query.ToQueryString;

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MultivariateEventCollectionContracts_AllowOmittedMembersForCompatibility()
    {
        var collection = JsonSerializer.Deserialize<MultivariateEventCollectionResponse>("{}", KalshiJsonOptions.Default);
        var page = JsonSerializer.Deserialize<MultivariateEventCollectionsResponse>("{}", KalshiJsonOptions.Default);
        var single = JsonSerializer.Deserialize<SingleMultivariateEventCollectionResponse>("{}", KalshiJsonOptions.Default);

        collection.Should().NotBeNull();
        collection!.AssociatedEvents.Should().BeEmpty();
        collection.PriceRanges.Should().BeEmpty();
        page!.Items.Should().BeEmpty();
        page.Cursor.Should().BeNull();
        single!.MultivariateContract.Should().BeNull();
    }

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
