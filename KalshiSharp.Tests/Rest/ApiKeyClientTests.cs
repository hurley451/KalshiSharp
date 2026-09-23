using System.Text.Json;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Errors;
using KalshiSharp.Http;
using KalshiSharp.Models.Common;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using KalshiSharp.Rest.ApiKeys;
using KalshiSharp.Serialization;
using KalshiSharp.Tests.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class ApiKeyClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly ApiKeyClient _client;
    private readonly IKalshiRequestSigner _signer;

    public ApiKeyClientTests()
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
        var signingHandler = new SigningDelegatingHandler(
            _signer,
            new SystemClock(),
            NullLogger<SigningDelegatingHandler>.Instance)
        {
            InnerHandler = new HttpClientHandler()
        };

        var httpClient = new HttpClient(signingHandler);
        var kalshiHttpClient = new KalshiHttpClient(
            httpClient,
            options,
            NullLogger<KalshiHttpClient>.Instance);

        _client = new ApiKeyClient(kalshiHttpClient);
    }

    public void Dispose()
    {
        _server.Dispose();
        (_signer as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task ListApiKeysAsync_DeserializesCurrentPayload()
    {
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/api_keys")
                .WithParam("fcm_subtrader_id", "user-1_suffix")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "api_keys": [
                        {
                            "api_key_id": "key-1",
                            "name": "readonly",
                            "scopes": ["read"],
                            "subaccount": 31,
                            "fcm_subtrader_id": "user-1_suffix"
                        }
                    ],
                    "api_key_region_expiration_ts": 1790000000
                }
                """));

        var result = await _client.ListApiKeysAsync(new ApiKeyQuery
        {
            FcmSubtraderId = "user-1_suffix"
        });

        result.ApiKeyRegionExpirationTs.Should().Be(1790000000);
        var key = result.ApiKeys.Should().ContainSingle().Which;
        key.ApiKeyId.Should().Be("key-1");
        key.Name.Should().Be("readonly");
        key.Scopes.Should().Equal(ApiKeyScopes.Read);
        key.Subaccount.Should().Be(31);
        key.FcmSubtraderId.Should().Be("user-1_suffix");
    }

    [Fact]
    public async Task CreateApiKeyAsync_SendsCallerManagedPublicKeyRequest()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new CreateApiKeyResponse { ApiKeyId = "key-2", Warning = "store key material securely" }
        };
        var client = new ApiKeyClient(httpClient);

        var response = await client.CreateApiKeyAsync(new CreateApiKeyRequest
        {
            Name = "writer",
            PublicKey = "-----BEGIN PUBLIC KEY-----\nabc\n-----END PUBLIC KEY-----",
            Scopes = [ApiKeyScopes.Read, ApiKeyScopes.Write],
            Subaccount = 2
        });

        response.ApiKeyId.Should().Be("key-2");
        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/api_keys");

        var json = JsonSerializer.Serialize(
            httpClient.LastRequest.Content!,
            httpClient.LastRequest.Content!.GetType(),
            KalshiJsonOptions.Default);
        json.Should().Contain("\"name\":\"writer\"");
        json.Should().Contain("\"public_key\":\"-----BEGIN PUBLIC KEY-----\\nabc\\n-----END PUBLIC KEY-----\"");
        json.Should().Contain("\"scopes\":[\"read\",\"write\"]");
        json.Should().Contain("\"subaccount\":2");
        json.Should().NotContain("fcm_subtrader_id");
    }

    [Fact]
    public async Task GenerateApiKeyAsync_SendsGenerateRequestAndRedactsToString()
    {
        var privateKey = "-----BEGIN PRIVATE KEY-----\nsecret\n-----END PRIVATE KEY-----";
        var httpClient = new RecordingHttpClient
        {
            Response = new GenerateApiKeyResponse
            {
                ApiKeyId = "key-3",
                PrivateKey = privateKey,
                Warning = "private key is returned once"
            }
        };
        var client = new ApiKeyClient(httpClient);

        var response = await client.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            Name = "generated-readonly",
            Scopes = [ApiKeyScopes.Read]
        });

        response.PrivateKey.Should().Be(privateKey);
        response.ToString().Should().NotContain(privateKey);
        response.ToString().Should().Contain("<redacted>");
        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/api_keys/generate");
        httpClient.LastRequest.RedactResponseContentInExceptions.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateApiKeyAsync_RedactsPrivateKeyFromDeserializationExceptionRawResponse()
    {
        var privateKey = "-----BEGIN PRIVATE KEY-----\nsecret\n-----END PRIVATE KEY-----";
        _server.Given(Request.Create()
                .WithPath("/trade-api/v2/api_keys/generate")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""
                {
                    "api_key_id": "key-3",
                    "private_key": "{{privateKey}}",
                    "warning": 123
                }
                """));

        var exception = await Assert.ThrowsAsync<KalshiException>(() => _client.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            Name = "generated-readonly",
            Scopes = [ApiKeyScopes.Read]
        }));

        exception.RawResponse.Should().Be("<redacted>");
        exception.ToString().Should().NotContain(privateKey);
    }

    [Fact]
    public async Task DeleteApiKeyAsync_UsesEscapedPathAndNoBody()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new object()
        };
        var client = new ApiKeyClient(httpClient);

        await client.DeleteApiKeyAsync("key/with space");

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/api_keys/key%2Fwith%20space");
        httpClient.LastRequest.Content.Should().BeNull();
    }

    [Fact]
    public async Task CreateAndGenerate_RejectDuplicateScopes()
    {
        var client = new ApiKeyClient(new RecordingHttpClient { Response = new object() });

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateApiKeyAsync(new CreateApiKeyRequest
        {
            Name = "duplicate",
            PublicKey = "public",
            Scopes = [ApiKeyScopes.Read, ApiKeyScopes.Read]
        }));

        await Assert.ThrowsAsync<ArgumentException>(() => client.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            Name = "duplicate",
            Scopes = [ApiKeyScopes.Write, ApiKeyScopes.Write]
        }));
    }

    [Fact]
    public async Task CreateAndGenerate_ValidateDocumentedScopeCombinations()
    {
        var client = new ApiKeyClient(new RecordingHttpClient { Response = new CreateApiKeyResponse { ApiKeyId = "key" } });

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateApiKeyAsync(new CreateApiKeyRequest
        {
            Name = "write-only",
            PublicKey = "public",
            Scopes = [ApiKeyScopes.Write]
        }));

        await Assert.ThrowsAsync<ArgumentException>(() => client.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            Name = "unknown",
            Scopes = ["admin"]
        }));

        await client.CreateApiKeyAsync(new CreateApiKeyRequest
        {
            Name = "child-scope",
            PublicKey = "public",
            Scopes = [ApiKeyScopes.WriteTrade]
        });
    }

    [Fact]
    public async Task CreateAndGenerate_ValidateRestrictionBindings()
    {
        var client = new ApiKeyClient(new RecordingHttpClient { Response = new object() });

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            Name = "bad-subaccount",
            Subaccount = 64
        }));

        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateApiKeyAsync(new CreateApiKeyRequest
        {
            Name = "both-bindings",
            PublicKey = "public",
            Subaccount = 1,
            FcmSubtraderId = "user-1_suffix"
        }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteApiKeyAsync_RejectsMissingApiKeyId(string apiKeyId)
    {
        var client = new ApiKeyClient(new RecordingHttpClient { Response = new object() });

        await Assert.ThrowsAsync<ArgumentException>(() => client.DeleteApiKeyAsync(apiKeyId));
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
