using System.Net;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Errors;
using KalshiSharp.Http;
using KalshiSharp.Rest.CfBenchmarks;
using KalshiSharp.Tests.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class CfBenchmarksClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly CfBenchmarksClient _client;

    public CfBenchmarksClientTests()
    {
        _server = WireMockServer.Start();
        var options = Options.Create(new KalshiClientOptions
        {
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret",
            BaseUri = new Uri(_server.Url!),
            Timeout = TimeSpan.FromSeconds(5)
        });
        var signingHandler = new SigningDelegatingHandler(
            new MockRequestSigner(options.Value.ApiKey, options.Value.ApiSecret),
            new SystemClock(),
            NullLogger<SigningDelegatingHandler>.Instance)
        {
            InnerHandler = new HttpClientHandler()
        };
        var httpClient = new KalshiHttpClient(
            new HttpClient(signingHandler),
            options,
            NullLogger<KalshiHttpClient>.Instance);

        _client = new CfBenchmarksClient(httpClient);
    }

    public void Dispose() => _server.Dispose();

    [Fact]
    public async Task GetCurrentAsync_PreservesRawProviderEnvelopeAndQuery()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/cfbenchmarks/values")
                .WithParam("id", "BRTI/USD")
                .WithHeader(MockRequestSigner.AccessKeyHeader, "test-api-key")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"data":{"serverTime":"2026-08-30T12:00:00Z","payload":{"value":"68000.12"},"future":true}}"""));

        var result = await _client.GetCurrentAsync("values", new Dictionary<string, string?>
        {
            ["id"] = "BRTI/USD",
            ["omitted"] = null
        });

        result.Data.GetProperty("payload").GetProperty("value").GetString().Should().Be("68000.12");
        result.Data.GetProperty("future").GetBoolean().Should().BeTrue();
        _server.LogEntries.Should().ContainSingle();
    }

    [Fact]
    public async Task GetHistoricalAsync_UsesExplicitHistoryRoute()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/cfbenchmarks/history/values")
                .WithParam("id", "BRTI")
                .WithParam("timespan", "HOUR")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"data":{"payload":{"ticks":[{"value":"68000.1234"}]}}}"""));

        var result = await _client.GetHistoricalAsync("values", new Dictionary<string, string?>
        {
            ["id"] = "BRTI",
            ["timespan"] = "HOUR"
        });

        result.Data.GetProperty("payload").GetProperty("ticks")[0]
            .GetProperty("value").GetString().Should().Be("68000.1234");
    }

    [Fact]
    public async Task GetCurrentAsync_EncodesEachNestedPathSegmentOnce()
    {
        var terminalHandler = new RecordingHandler();
        var options = Options.Create(new KalshiClientOptions
        {
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret",
            BaseUri = new Uri("https://example.test")
        });
        var signingHandler = new SigningDelegatingHandler(
            new MockRequestSigner(options.Value.ApiKey, options.Value.ApiSecret),
            new SystemClock(),
            NullLogger<SigningDelegatingHandler>.Instance)
        {
            InnerHandler = terminalHandler
        };
        using var httpClient = new HttpClient(signingHandler);
        var client = new CfBenchmarksClient(new KalshiHttpClient(
            httpClient,
            options,
            NullLogger<KalshiHttpClient>.Instance));

        var result = await client.GetCurrentAsync("provider data/values");

        result.Data.GetProperty("value").GetString().Should().Be("68000.12");
        terminalHandler.RequestUri.Should().NotBeNull();
        terminalHandler.RequestUri!.AbsolutePath.Should()
            .Be("/trade-api/v2/cfbenchmarks/provider%20data/values");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("/values")]
    [InlineData("values/")]
    [InlineData("values//latest")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("values/../latest")]
    [InlineData("values\\latest")]
    [InlineData("values?id=BRTI")]
    [InlineData("values#fragment")]
    [InlineData("values%2Flatest")]
    [InlineData("%2e%2e/values")]
    [InlineData("history/values")]
    public async Task GetCurrentAsync_RejectsUnsafeOrHistoricalPaths(string path)
    {
        var action = () => _client.GetCurrentAsync(path);

        await action.Should().ThrowAsync<ArgumentException>();
        _server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistoricalAsync_RejectsRepeatedHistorySegment()
    {
        var action = () => _client.GetHistoricalAsync("history/values");

        await action.Should().ThrowAsync<ArgumentException>();
        _server.LogEntries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(403, typeof(KalshiAuthException))]
    [InlineData(404, typeof(KalshiNotFoundException))]
    [InlineData(429, typeof(KalshiRateLimitException))]
    [InlineData(503, typeof(KalshiException))]
    public async Task GetCurrentAsync_PreservesStructuredKalshiErrors(int statusCode, Type exceptionType)
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/cfbenchmarks/values")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"upstream_error","message":"Upstream request failed"}"""));

        await Assert.ThrowsAsync(exceptionType, () => _client.GetCurrentAsync("values"));
    }

    [Fact]
    public async Task GetCurrentAsync_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var action = () => _client.GetCurrentAsync(
            "values",
            cancellationToken: cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":{"value":"68000.12"}}""")
            });
        }
    }
}
