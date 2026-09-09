using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using KalshiSharp.Rest.StructuredTargets;
using KalshiSharp.Serialization;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class StructuredTargetClientTests
{
    [Fact]
    public async Task GetStructuredTargetAsync_ReturnsFlexibleDetailsAndImageUrl()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = JsonSerializer.Deserialize<SingleStructuredTargetResponse>(
                """
                {
                  "structured_target": {
                    "id": "player-1",
                    "name": "Example Player",
                    "type": "basketball_player",
                    "details": {
                      "image_url": "https://example.com/player.png",
                      "league": "NBA",
                      "provider_data": {"active": true}
                    },
                    "source_id": "provider-1",
                    "source_ids": {"sportradar": "sr-1"},
                    "last_updated_ts": "2026-08-31T12:34:56Z"
                  }
                }
                """,
                KalshiJsonOptions.Default)!
        };
        var client = new StructuredTargetClient(httpClient);

        var result = await client.GetStructuredTargetAsync("player-1");

        httpClient.LastRequest!.Path.Should().Be("/trade-api/v2/structured_targets/player-1");
        result.Should().NotBeNull();
        result!.Details!.ImageUrl.Should().Be("https://example.com/player.png");
        result.Details.AdditionalProperties!["league"].GetString().Should().Be("NBA");
        result.Details.AdditionalProperties["provider_data"].GetProperty("active").GetBoolean().Should().BeTrue();
        result.SourceIds!["sportradar"].Should().Be("sr-1");
        result.LastUpdatedTs.Should().Be(
            DateTimeOffset.Parse("2026-08-31T12:34:56Z", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task GetStructuredTargetAsync_EscapesIdAndAllowsOmittedWrapperMember()
    {
        var httpClient = new RecordingHttpClient { Response = new SingleStructuredTargetResponse() };
        var client = new StructuredTargetClient(httpClient);

        var result = await client.GetStructuredTargetAsync("player/one");

        httpClient.LastRequest!.Path.Should().Be("/trade-api/v2/structured_targets/player%2Fone");
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetStructuredTargetAsync_RejectsMissingId(string id)
    {
        var client = new StructuredTargetClient(new RecordingHttpClient
        {
            Response = new SingleStructuredTargetResponse()
        });

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetStructuredTargetAsync(id));
    }

    [Fact]
    public async Task ListStructuredTargetsAsync_EmitsRepeatedIdsAndPagination()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new StructuredTargetsResponse
            {
                StructuredTargets = [new StructuredTargetResponse { Id = "one" }],
                Cursor = "next page"
            }
        };
        var client = new StructuredTargetClient(httpClient);

        var result = await client.ListStructuredTargetsAsync(new StructuredTargetQuery
        {
            Ids = ["player/1", "player 2"],
            Type = "basketball_player",
            Competition = "Pro Basketball (M)",
            PageSize = 2000,
            Cursor = "next page"
        });

        httpClient.LastRequest!.Path.Should().Be(
            "/trade-api/v2/structured_targets?ids=player%2F1&ids=player%202" +
            "&type=basketball_player&competition=Pro%20Basketball%20%28M%29" +
            "&page_size=2000&cursor=next%20page");
        result.Items.Should().ContainSingle().Which.Id.Should().Be("one");
        result.HasMore.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2001)]
    public void StructuredTargetQuery_RejectsInvalidPageSize(int pageSize)
    {
        var query = new StructuredTargetQuery { PageSize = pageSize };

        var action = query.ToQueryString;

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void StructuredTargetQuery_RejectsTooManyOrBlankIds()
    {
        var tooMany = new StructuredTargetQuery { Ids = Enumerable.Repeat("id", 2001).ToArray() };
        var blank = new StructuredTargetQuery { Ids = ["id", " "] };

        tooMany.Invoking(query => query.ToQueryString()).Should().Throw<ArgumentOutOfRangeException>();
        blank.Invoking(query => query.ToQueryString()).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StructuredTargetContracts_AllowEveryUndocumentedRequiredMemberToBeOmitted()
    {
        var target = JsonSerializer.Deserialize<StructuredTargetResponse>("{}", KalshiJsonOptions.Default);
        var page = JsonSerializer.Deserialize<StructuredTargetsResponse>("{}", KalshiJsonOptions.Default);
        var single = JsonSerializer.Deserialize<SingleStructuredTargetResponse>("{}", KalshiJsonOptions.Default);

        target.Should().NotBeNull();
        page!.Items.Should().BeEmpty();
        page.Cursor.Should().BeNull();
        single!.StructuredTarget.Should().BeNull();
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
