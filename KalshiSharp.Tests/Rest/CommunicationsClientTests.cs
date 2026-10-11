using FluentAssertions;
using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using KalshiSharp.Rest.Communications;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class CommunicationsClientTests
{
    [Fact]
    public async Task GetCommunicationsIdAsync_UsesDocumentedEndpointAndParsesResponse()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new CommunicationsIdResponse { CommunicationsId = "comm-123" }
        };
        var client = new CommunicationsClient(httpClient);

        var result = await client.GetCommunicationsIdAsync();

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Get);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/id");
        result.CommunicationsId.Should().Be("comm-123");
    }

    [Fact]
    public async Task ListRfqsAsync_UsesDocumentedFiltersAndParsesResponse()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new RfqsResponse
            {
                Rfqs =
                [
                    new RfqResponse
                    {
                        Id = "rfq-1",
                        CreatorId = "creator",
                        MarketTicker = "MARKET-ABC",
                        ContractsFp = "10.00",
                        Status = "open",
                        CreatedTs = DateTimeOffset.FromUnixTimeSeconds(1791720000)
                    }
                ],
                Cursor = "next"
            }
        };
        var client = new CommunicationsClient(httpClient);

        var result = await client.ListRfqsAsync(new RfqQuery
        {
            Limit = 25,
            Cursor = "cursor-1",
            EventTicker = "EVENT-ABC",
            MarketTicker = "MARKET-ABC",
            Subaccount = 3,
            Status = "open",
            UserFilter = CommunicationsUserFilter.Self
        });

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Get);
        httpClient.LastRequest.Path.Should().Be(
            "/trade-api/v2/communications/rfqs?limit=25&cursor=cursor-1&event_ticker=EVENT-ABC&market_ticker=MARKET-ABC&subaccount=3&status=open&user_filter=self");
        result.Items.Should().ContainSingle().Which.Id.Should().Be("rfq-1");
        result.Cursor.Should().Be("next");
    }

    [Fact]
    public async Task CreateRfqAsync_UsesDocumentedEndpointAndBody()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new CreateRfqResponse { Id = "rfq-1" }
        };
        var client = new CommunicationsClient(httpClient);
        var request = new CreateRfqRequest
        {
            MarketTicker = "MARKET-ABC",
            ContractsFp = "2.50",
            TargetCostDollars = "1.2500",
            TargetCostExcludesFees = true,
            ObscureCreatorId = true,
            RestRemainder = false,
            ReplaceExisting = true,
            Subaccount = 0
        };

        var result = await client.CreateRfqAsync(request);

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/rfqs");
        httpClient.LastRequest.Content.Should().BeSameAs(request);
        result.Id.Should().Be("rfq-1");
    }

    [Fact]
    public async Task GetAndDeleteRfqAsync_EscapeIdsInDocumentedPath()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new SingleRfqResponse
            {
                Rfq = new RfqResponse
                {
                    Id = "rfq/1",
                    CreatorId = "creator",
                    MarketTicker = "MARKET-ABC",
                    ContractsFp = "1.00",
                    Status = "open",
                    CreatedTs = DateTimeOffset.FromUnixTimeSeconds(1791720000)
                }
            }
        };
        var client = new CommunicationsClient(httpClient);

        await client.GetRfqAsync("rfq/1");

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Get);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/rfqs/rfq%2F1");

        await client.DeleteRfqAsync("rfq/1");

        httpClient.LastRequest.Method.Should().Be(HttpMethod.Delete);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/rfqs/rfq%2F1");
    }

    [Fact]
    public async Task ListQuotesAsync_UsesDocumentedFiltersAndParsesResponse()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new QuotesResponse
            {
                Quotes =
                [
                    new QuoteResponse
                    {
                        Id = "quote-1",
                        RfqId = "rfq-1",
                        CreatorId = "quote-creator",
                        RfqCreatorId = "rfq-creator",
                        MarketTicker = "MARKET-ABC",
                        ContractsFp = "3.00",
                        YesBidDollars = "0.5600",
                        NoBidDollars = "0.4400",
                        CreatedTs = DateTimeOffset.FromUnixTimeSeconds(1791720000),
                        UpdatedTs = DateTimeOffset.FromUnixTimeSeconds(1791720060),
                        Status = "open"
                    }
                ]
            }
        };
        var client = new CommunicationsClient(httpClient);

        var result = await client.ListQuotesAsync(new QuoteQuery
        {
            Limit = 500,
            MinTime = DateTimeOffset.FromUnixTimeSeconds(100),
            MaxTime = DateTimeOffset.FromUnixTimeSeconds(200),
            Status = "open",
            UserFilter = CommunicationsUserFilter.Self,
            RfqUserFilter = CommunicationsUserFilter.Self,
            RfqId = "rfq-1"
        });

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Get);
        httpClient.LastRequest.Path.Should().Be(
            "/trade-api/v2/communications/quotes?limit=500&min_ts=100&max_ts=200&status=open&user_filter=self&rfq_user_filter=self&rfq_id=rfq-1");
        result.Items.Should().ContainSingle().Which.Id.Should().Be("quote-1");
    }

    [Fact]
    public async Task CreateQuoteAsync_UsesDocumentedEndpointAndBody()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new CreateQuoteResponse { Id = "quote-1" }
        };
        var client = new CommunicationsClient(httpClient);
        var request = new CreateQuoteRequest
        {
            RfqId = "rfq-1",
            YesBid = "0.5600",
            NoBid = "0.4400",
            RestRemainder = true,
            PostOnly = true,
            Subaccount = 63
        };

        var result = await client.CreateQuoteAsync(request);

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/quotes");
        httpClient.LastRequest.Content.Should().BeSameAs(request);
        result.Id.Should().Be("quote-1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void RfqQuery_RejectsOutOfRangeLimit(int limit)
    {
        var query = new RfqQuery { Limit = limit };

        var action = () => query.ToQueryString();

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(nameof(RfqQuery.Limit));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public void QuoteQuery_RejectsOutOfRangeLimit(int limit)
    {
        var query = new QuoteQuery { Limit = limit };

        var action = () => query.ToQueryString();

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(nameof(QuoteQuery.Limit));
    }

    [Fact]
    public async Task CreateQuoteAsync_RejectsInvalidSubaccount()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new CreateQuoteResponse { Id = "quote-1" }
        };
        var client = new CommunicationsClient(httpClient);
        var request = new CreateQuoteRequest
        {
            RfqId = "rfq-1",
            YesBid = "0.5600",
            NoBid = "0.4400",
            RestRemainder = true,
            Subaccount = 64
        };

        var action = async () => await client.CreateQuoteAsync(request);

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("subaccount");
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
