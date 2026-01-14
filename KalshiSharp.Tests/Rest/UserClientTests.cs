using System.Globalization;
using System.Net;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Tests.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Errors;
using KalshiSharp.Http;
using KalshiSharp.Rest.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Rest;

/// <summary>
/// HTTP contract tests for the User client.
/// </summary>
public sealed class UserClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly UserClient _userClient;
    private readonly IKalshiRequestSigner _signer;

    public UserClientTests()
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
        var kalshiHttpClient = new KalshiHttpClient(
            httpClient,
            options,
            NullLogger<KalshiHttpClient>.Instance);

        _userClient = new UserClient(kalshiHttpClient);
    }

    public void Dispose()
    {
        _server.Dispose();
        (_signer as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task GetMeAsync_ReturnsUserProfile()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/users/me")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "user_id": "user-123",
                        "email": "test@example.com",
                        "username": "testuser"
                    }
                    """));

        // Act
        var result = await _userClient.GetMeAsync();

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be("user-123");
        result.Email.Should().Be("test@example.com");
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetMeAsync_WithAllFields_ReturnsCompleteProfile()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/users/me")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "user_id": "user-456",
                        "email": "fulluser@example.com",
                        "username": "fulluser",
                        "created_time": "2025-01-01T00:00:00Z",
                        "first_name": "John",
                        "last_name": "Doe",
                        "status": "active",
                        "is_verified": true,
                        "trading_tier": "standard"
                    }
                    """));

        // Act
        var result = await _userClient.GetMeAsync();

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be("user-456");
        result.Email.Should().Be("fulluser@example.com");
        result.Username.Should().Be("fulluser");
        result.CreatedTime.Should().Be(DateTimeOffset.Parse("2025-01-01T00:00:00Z", CultureInfo.InvariantCulture));
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Status.Should().Be("active");
        result.IsVerified.Should().BeTrue();
        result.TradingTier.Should().Be("standard");
    }

    [Fact]
    public async Task GetMeAsync_IncludesAuthHeaders()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/users/me")
                .WithHeader(MockRequestSigner.AccessKeyHeader, "test-api-key")
                .WithHeader(MockRequestSigner.AccessTimestampHeader, "*")
                .WithHeader(MockRequestSigner.AccessSignatureHeader, "*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                        "user_id": "user-123",
                        "email": "test@example.com",
                        "username": "testuser"
                    }
                    """));

        // Act
        var result = await _userClient.GetMeAsync();

        // Assert
        result.Should().NotBeNull();
        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMeAsync_SupportsCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _userClient.GetMeAsync(cts.Token));
    }

    [Fact]
    public async Task GetMeAsync_Returns401_ThrowsKalshiAuthException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/users/me")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"error": {"code": "unauthorized", "message": "Invalid credentials"}}"""));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiAuthException>(
            () => _userClient.GetMeAsync());
        exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMeAsync_Returns403_ThrowsKalshiAuthException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/users/me")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(403)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"error": {"code": "forbidden", "message": "Access denied"}}"""));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KalshiAuthException>(
            () => _userClient.GetMeAsync());
        exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
