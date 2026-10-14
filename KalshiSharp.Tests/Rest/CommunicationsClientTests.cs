using System.Text.Json;
using FluentAssertions;
using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using KalshiSharp.Rest.Communications;
using KalshiSharp.Serialization;
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
    public async Task ListBlockTradeProposalsAsync_UsesDocumentedFiltersAndParsesResponse()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new BlockTradeProposalsResponse
            {
                BlockTradeProposals =
                [
                    new BlockTradeProposalResponse
                    {
                        Id = "proposal-1",
                        ProposerUserId = "proposer",
                        BuyerUserId = "buyer",
                        SellerUserId = "seller",
                        MarketTicker = "MARKET-ABC",
                        PriceCentiCents = 5600,
                        Centicount = 10000,
                        MakerSide = "yes",
                        ExpirationTs = DateTimeOffset.FromUnixTimeSeconds(1791723600),
                        Status = "open",
                        CreatedTs = DateTimeOffset.FromUnixTimeSeconds(1791720000),
                        UpdatedTs = DateTimeOffset.FromUnixTimeSeconds(1791720060)
                    }
                ],
                Cursor = "next"
            }
        };
        var client = new CommunicationsClient(httpClient);

        var result = await client.ListBlockTradeProposalsAsync(new BlockTradeProposalQuery
        {
            Limit = 100,
            Cursor = "cursor-1",
            MarketTicker = "MARKET-ABC",
            Status = "open"
        });

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Get);
        httpClient.LastRequest.Path.Should().Be(
            "/trade-api/v2/communications/block-trade-proposals?limit=100&cursor=cursor-1&market_ticker=MARKET-ABC&status=open");
        result.Items.Should().ContainSingle().Which.Id.Should().Be("proposal-1");
        result.Cursor.Should().Be("next");
    }

    [Fact]
    public async Task ProposeBlockTradeAsync_UsesDocumentedEndpointAndBody()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new ProposeBlockTradeResponse { BlockTradeProposalId = "proposal-1" }
        };
        var client = new CommunicationsClient(httpClient);
        var request = new ProposeBlockTradeRequest
        {
            BuyerUserId = "buyer",
            SellerUserId = "seller",
            MarketTicker = "MARKET-ABC",
            PriceCentiCents = 5600,
            Centicount = 10000,
            MakerSide = "yes",
            ExpirationTs = DateTimeOffset.FromUnixTimeSeconds(1791723600),
            BuyerSubaccount = 0,
            SellerSubtraderId = "seller-subtrader"
        };

        var result = await client.ProposeBlockTradeAsync(request);

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/block-trade-proposals");
        httpClient.LastRequest.Content.Should().BeSameAs(request);
        using var body = JsonDocument.Parse(JsonSerializer.Serialize(
            httpClient.LastRequest.Content,
            httpClient.LastRequest.Content!.GetType(),
            KalshiJsonOptions.Default));
        body.RootElement.GetProperty("expiration_ts").GetString()
            .Should().Be("2026-10-11T13:00:00.0000000Z");
        result.BlockTradeProposalId.Should().Be("proposal-1");
    }

    [Fact]
    public async Task AcceptBlockTradeProposalAsync_EscapesIdAndSendsOptionalBody()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new ProposeBlockTradeResponse { BlockTradeProposalId = "unused" }
        };
        var client = new CommunicationsClient(httpClient);
        var request = new AcceptBlockTradeProposalRequest { Subaccount = 31 };

        await client.AcceptBlockTradeProposalAsync("proposal/1", request);

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/block-trade-proposals/proposal%2F1/accept");
        httpClient.LastRequest.Content.Should().BeSameAs(request);

        await client.AcceptBlockTradeProposalAsync("proposal-2");

        httpClient.LastRequest.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/block-trade-proposals/proposal-2/accept");
        httpClient.LastRequest.Content.Should().BeNull();
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

    [Fact]
    public async Task GetAndDeleteQuoteAsync_UseScopedDocumentedPath()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new SingleQuoteResponse
            {
                Quote = CreateQuote("rfq/1", "quote/1")
            }
        };
        var client = new CommunicationsClient(httpClient);

        var result = await client.GetQuoteAsync("rfq/1", "quote/1");

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Get);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/rfqs/rfq%2F1/quotes/quote%2F1");
        result.Quote.Id.Should().Be("quote/1");

        await client.DeleteQuoteAsync("rfq/1", "quote/1");

        httpClient.LastRequest.Method.Should().Be(HttpMethod.Delete);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/rfqs/rfq%2F1/quotes/quote%2F1");
    }

    [Fact]
    public async Task AcceptQuoteAsync_UsesScopedDocumentedPathAndBody()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new CreateQuoteResponse { Id = "unused" }
        };
        var client = new CommunicationsClient(httpClient);
        var request = new AcceptQuoteRequest { AcceptedSide = "yes" };

        await client.AcceptQuoteAsync("rfq-1", "quote-1", request);

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Put);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/rfqs/rfq-1/quotes/quote-1/accept");
        httpClient.LastRequest.Content.Should().BeSameAs(request);
    }

    [Fact]
    public async Task ConfirmQuoteAsync_UsesScopedDocumentedPathWithoutBody()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new CreateQuoteResponse { Id = "unused" }
        };
        var client = new CommunicationsClient(httpClient);

        await client.ConfirmQuoteAsync("rfq-1", "quote-1");

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Put);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/rfqs/rfq-1/quotes/quote-1/confirm");
        httpClient.LastRequest.Content.Should().BeNull();
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

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void BlockTradeProposalQuery_RejectsOutOfRangeLimit(int limit)
    {
        var query = new BlockTradeProposalQuery { Limit = limit };

        var action = () => query.ToQueryString();

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(BlockTradeProposalQuery.Limit));
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

    [Theory]
    [InlineData("YES")]
    [InlineData("buy")]
    [InlineData("")]
    public async Task AcceptQuoteAsync_RejectsInvalidAcceptedSide(string acceptedSide)
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new CreateQuoteResponse { Id = "unused" }
        };
        var client = new CommunicationsClient(httpClient);
        var request = new AcceptQuoteRequest { AcceptedSide = acceptedSide };

        var action = async () => await client.AcceptQuoteAsync("rfq-1", "quote-1", request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("request");
    }

    [Theory]
    [InlineData(0, 10000, "yes")]
    [InlineData(5600, 0, "yes")]
    [InlineData(5600, 10000, "YES")]
    public async Task ProposeBlockTradeAsync_ValidatesDocumentedBoundaries(
        long priceCentiCents,
        long centicount,
        string makerSide)
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new ProposeBlockTradeResponse { BlockTradeProposalId = "unused" }
        };
        var client = new CommunicationsClient(httpClient);
        var request = new ProposeBlockTradeRequest
        {
            BuyerUserId = "buyer",
            SellerUserId = "seller",
            MarketTicker = "MARKET-ABC",
            PriceCentiCents = priceCentiCents,
            Centicount = centicount,
            MakerSide = makerSide,
            ExpirationTs = DateTimeOffset.FromUnixTimeSeconds(1791723600)
        };

        var action = async () => await client.ProposeBlockTradeAsync(request);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProposeBlockTradeAsync_RejectsAmbiguousSubtraderAndSubaccount()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new ProposeBlockTradeResponse { BlockTradeProposalId = "unused" }
        };
        var client = new CommunicationsClient(httpClient);
        var request = new ProposeBlockTradeRequest
        {
            BuyerUserId = "buyer",
            SellerUserId = "seller",
            MarketTicker = "MARKET-ABC",
            PriceCentiCents = 5600,
            Centicount = 10000,
            MakerSide = "yes",
            ExpirationTs = DateTimeOffset.FromUnixTimeSeconds(1791723600),
            BuyerSubtraderId = "buyer-subtrader",
            BuyerSubaccount = 1
        };

        var action = async () => await client.ProposeBlockTradeAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("buyer");
    }

    [Fact]
    public async Task AcceptBlockTradeProposalAsync_RejectsAmbiguousAcceptanceIdentity()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new ProposeBlockTradeResponse { BlockTradeProposalId = "unused" }
        };
        var client = new CommunicationsClient(httpClient);
        var request = new AcceptBlockTradeProposalRequest
        {
            SubtraderId = "subtrader",
            Subaccount = 1
        };

        var action = async () => await client.AcceptBlockTradeProposalAsync("proposal-1", request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("acceptance");
    }

    private static QuoteResponse CreateQuote(string rfqId, string quoteId) =>
        new()
        {
            Id = quoteId,
            RfqId = rfqId,
            CreatorId = "quote-creator",
            RfqCreatorId = "rfq-creator",
            MarketTicker = "MARKET-ABC",
            ContractsFp = "3.00",
            YesBidDollars = "0.5600",
            NoBidDollars = "0.4400",
            CreatedTs = DateTimeOffset.FromUnixTimeSeconds(1791720000),
            UpdatedTs = DateTimeOffset.FromUnixTimeSeconds(1791720060),
            Status = "open"
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
