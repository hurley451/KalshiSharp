using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Rest.OrderGroups;
using KalshiSharp.Tests.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class OrderGroupClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly OrderGroupClient _client;
    private readonly IKalshiRequestSigner _signer;

    public OrderGroupClientTests()
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
        var handler = new SigningDelegatingHandler(
            _signer,
            new SystemClock(),
            NullLogger<SigningDelegatingHandler>.Instance)
        {
            InnerHandler = new HttpClientHandler()
        };

        var httpClient = new HttpClient(handler);
        var kalshiHttpClient = new KalshiHttpClient(
            httpClient,
            options,
            NullLogger<KalshiHttpClient>.Instance);

        _client = new OrderGroupClient(kalshiHttpClient);
    }

    public void Dispose()
    {
        _server.Dispose();
        (_signer as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task ListOrderGroupsAsync_AppliesPaginationAndSubaccountFilter()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/order_groups")
                .WithParam("limit", "10")
                .WithParam("cursor", "next")
                .WithParam("subaccount", "2")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                      "order_groups": [
                        {
                          "order_group_id": "og-1",
                          "status": "open",
                          "triggered": false,
                          "exchange_index": 1,
                          "subaccount": 2,
                          "contracts_limit": 100,
                          "contracts_limit_fp": "100.00",
                          "order_ids": ["order-1"],
                          "created_ts_ms": 1704067200000,
                          "updated_ts_ms": 1704067201000
                        }
                      ],
                      "cursor": "after"
                    }
                    """));

        var result = await _client.ListOrderGroupsAsync(new OrderGroupQuery
        {
            Limit = 10,
            Cursor = "next",
            Subaccount = 2
        });

        result.Items.Should().ContainSingle();
        result.Items[0].OrderGroupId.Should().Be("og-1");
        result.Items[0].ContractsLimitFp.Should().Be("100.00");
        result.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrderGroupAsync_GetsGroupByIdentifier()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/order_groups/og-1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"order_group":{"order_group_id":"og-1","status":"open"}}"""));

        var result = await _client.GetOrderGroupAsync("og-1");

        result.OrderGroup!.OrderGroupId.Should().Be("og-1");
    }

    [Fact]
    public async Task GetOrderGroupAsync_RejectsEmptyIdentifier()
    {
        var action = () => _client.GetOrderGroupAsync(" ");

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrderGroupAsync_SendsCurrentBodyShape()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/order_groups/create")
                .UsingPost()
                .WithBody(body =>
                    body!.Contains("\"exchange_index\":1", StringComparison.Ordinal) &&
                    body.Contains("\"subaccount\":0", StringComparison.Ordinal) &&
                    body.Contains("\"contracts_limit_fp\":\"100.00\"", StringComparison.Ordinal)))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"order_group_id":"og-1","subaccount":0,"exchange_index":1}"""));

        var result = await _client.CreateOrderGroupAsync(new CreateOrderGroupRequest
        {
            ExchangeIndex = 1,
            Subaccount = 0,
            ContractsLimitFp = "100.00"
        });

        result.OrderGroupId.Should().Be("og-1");
    }

    [Fact]
    public async Task UpdateOrderGroupAsync_SendsPutToLimitEndpoint()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/portfolio/order_groups/og-1/limit")
                .UsingPut()
                .WithBody(body => body!.Contains("\"contracts_limit\":25", StringComparison.Ordinal)))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"order_group":{"order_group_id":"og-1","contracts_limit":25}}"""));

        var result = await _client.UpdateOrderGroupAsync("og-1", new UpdateOrderGroupRequest
        {
            ContractsLimit = 25
        });

        result.OrderGroup!.ContractsLimit.Should().Be(25);
    }

    [Theory]
    [InlineData("trigger", "PUT")]
    [InlineData("reset", "PUT")]
    [InlineData("", "DELETE")]
    public async Task ActionMethods_SendActionBody(string action, string method)
    {
        var path = string.IsNullOrEmpty(action)
            ? "/trade-api/v2/portfolio/order_groups/og-1"
            : $"/trade-api/v2/portfolio/order_groups/og-1/{action}";

        _server.Given(Request.Create()
                .WithPath(path)
                .UsingMethod(method)
                .WithBody(body =>
                    body!.Contains("\"exchange_index\":3", StringComparison.Ordinal) &&
                    body.Contains("\"subaccount\":63", StringComparison.Ordinal)))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{}"));

        var request = new TriggerOrderGroupRequest { ExchangeIndex = 3, Subaccount = 63 };

        if (action == "trigger")
        {
            await _client.TriggerOrderGroupAsync("og-1", request);
        }
        else if (action == "reset")
        {
            await _client.ResetOrderGroupAsync("og-1", new ResetOrderGroupRequest { ExchangeIndex = 3, Subaccount = 63 });
        }
        else
        {
            await _client.DeleteOrderGroupAsync("og-1", new DeleteOrderGroupRequest { ExchangeIndex = 3, Subaccount = 63 });
        }
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 64)]
    public async Task WriteMethods_RejectInvalidShardOrSubaccount(int exchangeIndex, int subaccount)
    {
        var action = () => _client.CreateOrderGroupAsync(new CreateOrderGroupRequest
        {
            ExchangeIndex = exchangeIndex,
            Subaccount = subaccount,
            ContractsLimit = 1
        });

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(64)]
    public void Query_RejectsInvalidSubaccount(int subaccount)
    {
        var action = () => new OrderGroupQuery { Subaccount = subaccount }.ToQueryString();

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task CreateOrderGroupAsync_RequiresContractLimit()
    {
        var action = () => _client.CreateOrderGroupAsync(new CreateOrderGroupRequest());

        await action.Should().ThrowAsync<ArgumentException>();
    }
}
