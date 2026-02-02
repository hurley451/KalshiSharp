using System.Net;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Tests.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Errors;
using KalshiSharp.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Http;

/// <summary>
/// HTTP contract tests using WireMock to validate request/response handling.
/// </summary>
public sealed class HttpContractTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly KalshiHttpClient _client;
    private readonly IKalshiRequestSigner _signer;

    public HttpContractTests()
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
        var clock = new SystemClock();

        var signingHandler = new SigningDelegatingHandler(
            _signer,
            clock,
            NullLogger<SigningDelegatingHandler>.Instance)
        {
            InnerHandler = new HttpClientHandler()
        };

        var httpClient = new HttpClient(signingHandler);
        _client = new KalshiHttpClient(
            httpClient,
            options,
            NullLogger<KalshiHttpClient>.Instance);
    }

    public void Dispose()
    {
        _server.Dispose();
        (_signer as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task SendAsync_WithSuccessResponse_DeserializesCorrectly()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/test").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"name":"test","value":42}"""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/test"
        };

        // Act
        var result = await _client.SendAsync<TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task SendAsync_WithQueryParameters_AppendsQueryString()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/test")
                .WithParam("limit", "10")
                .WithParam("cursor", "abc123")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"name":"test","value":1}"""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/test",
            QueryParameters = new Dictionary<string, string?>
            {
                ["limit"] = "10",
                ["cursor"] = "abc123",
                ["empty"] = null // Should be excluded
            }
        };

        // Act
        var result = await _client.SendAsync<TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_WithRequestBody_SerializesJson()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/orders")
                .WithBody(body => body != null && body.Contains("\"ticker\"") && body.Contains("\"MARKET-ABC\""))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"name":"order-created","value":100}"""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = "/orders",
            Content = new { Ticker = "MARKET-ABC", Quantity = 10 }
        };

        // Act
        var result = await _client.SendAsync<TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("order-created");
    }

    [Fact]
    public async Task SendAsync_IncludesSigningHeaders()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/authenticated")
                .WithHeader(MockRequestSigner.AccessKeyHeader, "test-api-key")
                .WithHeader(MockRequestSigner.AccessTimestampHeader, "*")
                .WithHeader(MockRequestSigner.AccessSignatureHeader, "*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"name":"authenticated","value":1}"""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/authenticated"
        };

        // Act
        var result = await _client.SendAsync<TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_IncludesRequestIdHeader()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/tracked")
                .WithHeader(KalshiHttpClient.RequestIdHeader, "*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"name":"tracked","value":1}"""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/tracked"
        };

        // Act
        var result = await _client.SendAsync<TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public async Task SendAsync_WithAuthError_ThrowsKalshiAuthException(int statusCode)
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/auth-error").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"auth_error","message":"Authentication failed"}"""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/auth-error"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiAuthException>(
            () => _client.SendAsync<TestResponse>(request));

        exception.StatusCode.Should().Be((HttpStatusCode)statusCode);
        exception.ErrorCode.Should().Be("auth_error");
        exception.Message.Should().Contain("Authentication failed");
    }

    [Fact]
    public async Task SendAsync_WithNotFoundError_ThrowsKalshiNotFoundException()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/not-found").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"not_found","message":"Resource not found"}"""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/not-found"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiNotFoundException>(
            () => _client.SendAsync<TestResponse>(request));

        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendAsync_WithValidationError_ThrowsKalshiValidationException()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/validation-error").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"validation_error","message":"Validation failed","errors":{"quantity":["must be positive"]}}"""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Post,
            Path = "/validation-error",
            Content = new { Quantity = -1 }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiValidationException>(
            () => _client.SendAsync<TestResponse>(request));

        exception.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        exception.ValidationErrors.Should().ContainKey("quantity");
    }

    [Fact]
    public async Task SendAsync_WithRateLimitError_ThrowsKalshiRateLimitException()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/rate-limited").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("Retry-After", "30")
                .WithBody("""{"code":"rate_limited","message":"Rate limit exceeded"}"""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/rate-limited"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiRateLimitException>(
            () => _client.SendAsync<TestResponse>(request));

        exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task SendAsync_WithServerError_ThrowsKalshiException()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/server-error").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"code":"internal_error","message":"Internal server error"}"""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/server-error"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiException>(
            () => _client.SendAsync<TestResponse>(request));

        exception.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        exception.Should().NotBeOfType<KalshiAuthException>();
        exception.Should().NotBeOfType<KalshiRateLimitException>();
    }

    [Fact]
    public async Task SendAsync_WithEmptyResponseBody_ThrowsException()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/empty").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/empty"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiException>(
            () => _client.SendAsync<TestResponse>(request));

        exception.Message.Should().Contain("empty");
    }

    [Fact]
    public async Task SendAsync_WithMalformedJson_ThrowsException()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/malformed").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("not json"));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/malformed"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiException>(
            () => _client.SendAsync<TestResponse>(request));

        exception.Message.Should().Contain("deserialize");
    }

    [Fact]
    public async Task SendAsync_VoidOverload_DoesNotExpectBody()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/delete").UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(204));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Delete,
            Path = "/delete"
        };

        // Act & Assert - should not throw
        await _client.SendAsync(request);
    }

    [Fact]
    public async Task SendAsync_ExceptionIncludesRequestId()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/error-with-id").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("""{"code":"error","message":"Error"}"""));

        var request = new KalshiRequest
        {
            Method = HttpMethod.Get,
            Path = "/error-with-id"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiException>(
            () => _client.SendAsync<TestResponse>(request));

        exception.RequestId.Should().NotBeNullOrEmpty();
        exception.RequestId.Should().HaveLength(32); // Guid without hyphens
    }

    /// <summary>
    /// Test response type for deserialization tests.
    /// </summary>
    private sealed record TestResponse
    {
        public string? Name { get; init; }
        public int Value { get; init; }
    }
}
