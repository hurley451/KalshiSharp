using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using KalshiSharp.Rest.Milestones;
using KalshiSharp.Serialization;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class MilestoneClientTests
{
    [Fact]
    public async Task GetMilestoneAsync_ReturnsCurrentWireShapeWithOptionalFields()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = JsonSerializer.Deserialize<SingleMilestoneResponse>(
                """
                {
                  "milestone": {
                    "id": "game-1",
                    "category": "Sports",
                    "type": "basketball_game",
                    "start_date": "2026-09-01T19:00:00Z",
                    "related_event_tickers": ["EVENT-1"],
                    "title": "Example Game",
                    "notification_message": "Game started",
                    "details": {"home_team": "One", "away_team": "Two"},
                    "primary_event_tickers": ["EVENT-1-MAIN"],
                    "last_updated_ts": "2026-09-01T18:55:00Z",
                    "end_date": null,
                    "source_id": null
                  }
                }
                """,
                KalshiJsonOptions.Default)!
        };
        var client = new MilestoneClient(httpClient);

        var result = await client.GetMilestoneAsync("game-1");

        httpClient.LastRequest!.Path.Should().Be("/trade-api/v2/milestones/game-1");
        result.Id.Should().Be("game-1");
        result.RelatedEventTickers.Should().Equal("EVENT-1");
        result.PrimaryEventTickers.Should().Equal("EVENT-1-MAIN");
        result.Details.GetProperty("home_team").GetString().Should().Be("One");
        result.EndDate.Should().BeNull();
        result.SourceId.Should().BeNull();
        result.SourceIds.Should().BeNull();
    }

    [Fact]
    public async Task GetMilestoneAsync_EscapesId()
    {
        var httpClient = new RecordingHttpClient { Response = CreateSingleResponse() };
        var client = new MilestoneClient(httpClient);

        await client.GetMilestoneAsync("game/one");

        httpClient.LastRequest!.Path.Should().Be("/trade-api/v2/milestones/game%2Fone");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetMilestoneAsync_RejectsMissingId(string id)
    {
        var client = new MilestoneClient(new RecordingHttpClient { Response = CreateSingleResponse() });

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetMilestoneAsync(id));
    }

    [Fact]
    public async Task ListMilestonesAsync_EmitsEveryDocumentedFilter()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new MilestonesResponse { Milestones = [] }
        };
        var client = new MilestoneClient(httpClient);

        await client.ListMilestonesAsync(new MilestoneQuery
        {
            Limit = 500,
            MinimumStartDate = DateTimeOffset.Parse("2026-09-01T12:34:56Z", CultureInfo.InvariantCulture),
            Category = "Sports",
            Competition = "Pro Basketball (M)",
            SourceId = "provider/1",
            Type = "basketball_game",
            RelatedEventTicker = "EVENT ONE",
            Cursor = "next page",
            MinUpdatedTs = DateTimeOffset.FromUnixTimeSeconds(1788264000)
        });

        httpClient.LastRequest!.Path.Should().Be(
            "/trade-api/v2/milestones?limit=500" +
            "&minimum_start_date=2026-09-01T12%3A34%3A56.0000000Z" +
            "&category=Sports&competition=Pro%20Basketball%20%28M%29" +
            "&source_id=provider%2F1&type=basketball_game" +
            "&related_event_ticker=EVENT%20ONE&cursor=next%20page" +
            "&min_updated_ts=1788264000");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public void MilestoneQuery_RejectsInvalidRequiredLimit(int limit)
    {
        var query = new MilestoneQuery { Limit = limit };

        query.Invoking(value => value.ToQueryString()).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("category")]
    [InlineData("type")]
    [InlineData("start_date")]
    [InlineData("related_event_tickers")]
    [InlineData("title")]
    [InlineData("notification_message")]
    [InlineData("details")]
    [InlineData("primary_event_tickers")]
    [InlineData("last_updated_ts")]
    public void MilestoneResponse_RejectsMissingRequiredMember(string memberName)
    {
        var json = JsonNode.Parse(CreateMilestoneJson())!.AsObject();
        json.Remove(memberName);

        var action = () => JsonSerializer.Deserialize<MilestoneResponse>(
            json.ToJsonString(),
            KalshiJsonOptions.Default);

        action.Should().Throw<JsonException>();
    }

    [Fact]
    public void MilestoneWrappers_RejectMissingRequiredMembers()
    {
        var listAction = () => JsonSerializer.Deserialize<MilestonesResponse>("{}", KalshiJsonOptions.Default);
        var singleAction = () => JsonSerializer.Deserialize<SingleMilestoneResponse>("{}", KalshiJsonOptions.Default);

        listAction.Should().Throw<JsonException>();
        singleAction.Should().Throw<JsonException>();
    }

    private static SingleMilestoneResponse CreateSingleResponse() => new()
    {
        Milestone = JsonSerializer.Deserialize<MilestoneResponse>(
            CreateMilestoneJson(),
            KalshiJsonOptions.Default)!
    };

    private static string CreateMilestoneJson() =>
        """
        {
          "id": "game-1",
          "category": "Sports",
          "type": "basketball_game",
          "start_date": "2026-09-01T19:00:00Z",
          "related_event_tickers": [],
          "title": "Example Game",
          "notification_message": "Game started",
          "details": {},
          "primary_event_tickers": [],
          "last_updated_ts": "2026-09-01T18:55:00Z"
        }
        """;

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
