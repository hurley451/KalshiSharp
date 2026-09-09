using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using FluentAssertions;
using KalshiSharp.DependencyInjection;
using KalshiSharp.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace KalshiSharp.Tests.DependencyInjection;

public sealed class HttpPipelineTests
{
    [Fact]
    public async Task DependencyInjectedClient_ChargesCancelAllForEveryPhysicalRetry()
    {
        using var rsa = RSA.Create();
        var limiter = new RecordingRateLimiter();
        var terminalHandler = new SequencedResponseHandler(
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.OK);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKalshiClient(options =>
        {
            options.ApiKey = "test-api-key";
            options.ApiSecret = rsa.ExportRSAPrivateKeyPem();
        });
        services.Replace(ServiceDescriptor.Singleton<IRateLimiter>(limiter));
        services.AddHttpClient(ServiceCollectionExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => terminalHandler);

        using var provider = services.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
        using var client = clientFactory.CreateClient(ServiceCollectionExtensions.HttpClientName);
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            "https://example.test/trade-api/v2/portfolio/events/orders?subaccount=3");

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        terminalHandler.SendCount.Should().Be(2);
        limiter.Requests.Should().HaveCount(2);
        limiter.Requests.Should().OnlyContain(classification =>
            classification.IsWrite && classification.IsBatch && classification.TokenCost == 2);
    }

    private sealed class RecordingRateLimiter : IRateLimiter
    {
        public ConcurrentQueue<RateLimitRequest> Requests { get; } = new();

        public bool IsThrottling => false;

        public ValueTask WaitAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask WaitAsync(
            RateLimitRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Enqueue(request);
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class SequencedResponseHandler(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        private int _sendCount;

        public int SendCount => _sendCount;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref _sendCount);
            var status = statuses[Math.Min(attempt - 1, statuses.Length - 1)];
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}
