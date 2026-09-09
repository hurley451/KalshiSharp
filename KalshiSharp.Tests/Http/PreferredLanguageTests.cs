using System.Security.Cryptography;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.DependencyInjection;
using KalshiSharp.Http;
using KalshiSharp.Rest;
using KalshiSharp.Tests.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Http;

public sealed class PreferredLanguageTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    public void Dispose()
    {
        _server.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task DefaultConfiguration_OmitsAcceptLanguageHeader()
    {
        _server.Given(Request.Create()
                .WithPath("/test")
                .WithHeader(headers => !headers.ContainsKey("Accept-Language"))
                .UsingGet())
            .RespondWith(JsonResponse("""{"name":"english"}"""));

        using var httpClient = CreateHttpClient();
        var client = CreateKalshiHttpClient(httpClient, preferredLanguage: null);

        await client.SendAsync<TestResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/test"
        });

        _server.LogEntries.Should().ContainSingle();
        _server.LogEntries.Single().MappingGuid.Should().NotBeNull();
    }

    [Theory]
    [InlineData("es")]
    [InlineData("es-MX")]
    [InlineData("pt")]
    [InlineData("pt-BR")]
    [InlineData("I-KLINGON")]
    [InlineData("X-example")]
    public async Task ConfiguredLanguage_SendsOneLanguageTagAlongsideSigningHeaders(string preferredLanguage)
    {
        _server.Given(Request.Create()
                .WithPath("/test")
                .WithHeader("Accept-Language", preferredLanguage)
                .WithHeader(MockRequestSigner.AccessKeyHeader, "test-api-key")
                .WithHeader(MockRequestSigner.AccessTimestampHeader, "*")
                .WithHeader(MockRequestSigner.AccessSignatureHeader, "*")
                .UsingGet())
            .RespondWith(JsonResponse("""{"name":"localized"}"""));

        using var httpClient = CreateHttpClient();
        var client = CreateKalshiHttpClient(httpClient, preferredLanguage);

        await client.SendAsync<TestResponse>(new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/test"
        });

        _server.LogEntries.Should().ContainSingle();
        _server.LogEntries.Single().MappingGuid.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("es,pt")]
    [InlineData("es;q=0.8")]
    [InlineData("*")]
    [InlineData("not_a_language")]
    [InlineData("es\r\nX-Test: injected")]
    [InlineData("en-1901-1901")]
    [InlineData("en-a-foo-a-bar")]
    [InlineData("Kk")]
    public void InvalidLanguage_FailsDuringClientConfiguration(string preferredLanguage)
    {
        using var httpClient = CreateHttpClient();

        var act = () => CreateKalshiHttpClient(httpClient, preferredLanguage);

        act.Should().Throw<ArgumentException>()
            .Where(exception => exception.ParamName == "preferredLanguage")
            .WithMessage("*valid BCP 47 language tag*");
        _server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task DirectClient_AppliesConfiguredLanguage()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/exchange/status")
                .WithHeader("Accept-Language", "es-MX")
                .UsingGet())
            .RespondWith(JsonResponse("""{"exchange_active":true,"trading_active":true}"""));

        using var rsa = RSA.Create(2048);
        using var client = new KalshiClient(new KalshiClientOptions
        {
            ApiKey = "test-api-key",
            ApiSecret = rsa.ExportPkcs8PrivateKeyPem(),
            BaseUri = new Uri(_server.Url!),
            EnableRateLimiting = false,
            PreferredLanguage = "es-MX"
        });

        await client.Exchange.GetStatusAsync();

        _server.LogEntries.Should().ContainSingle();
        _server.LogEntries.Single().MappingGuid.Should().NotBeNull();
    }

    [Fact]
    public async Task DependencyInjectedClient_AppliesConfiguredLanguage()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/exchange/status")
                .WithHeader("Accept-Language", "pt-BR")
                .UsingGet())
            .RespondWith(JsonResponse("""{"exchange_active":true,"trading_active":true}"""));

        using var rsa = RSA.Create(2048);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKalshiClient(options =>
        {
            options.ApiKey = "test-api-key";
            options.ApiSecret = rsa.ExportPkcs8PrivateKeyPem();
            options.BaseUri = new Uri(_server.Url!);
            options.EnableRateLimiting = false;
            options.PreferredLanguage = "pt-BR";
        });

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IKalshiClient>();

        await client.Exchange.GetStatusAsync();

        _server.LogEntries.Should().ContainSingle();
        _server.LogEntries.Single().MappingGuid.Should().NotBeNull();
    }

    private static HttpClient CreateHttpClient()
    {
        var signer = new MockRequestSigner("test-api-key", "test-api-secret");
        var signingHandler = new SigningDelegatingHandler(
            signer,
            new SystemClock(),
            NullLogger<SigningDelegatingHandler>.Instance)
        {
            InnerHandler = new HttpClientHandler()
        };

        return new HttpClient(signingHandler);
    }

    private KalshiHttpClient CreateKalshiHttpClient(HttpClient httpClient, string? preferredLanguage)
    {
        var options = Options.Create(new KalshiClientOptions
        {
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret",
            BaseUri = new Uri(_server.Url!),
            PreferredLanguage = preferredLanguage
        });

        return new KalshiHttpClient(
            httpClient,
            options,
            NullLogger<KalshiHttpClient>.Instance);
    }

    private static IResponseBuilder JsonResponse(string body) =>
        Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(body);

    private sealed record TestResponse
    {
        public string? Name { get; init; }
    }
}
