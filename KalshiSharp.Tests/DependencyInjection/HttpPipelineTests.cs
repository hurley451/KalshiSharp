using System.Net;
using FluentAssertions;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.DependencyInjection;
using KalshiSharp.RateLimiting;
using KalshiSharp.Rest;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KalshiSharp.Tests.DependencyInjection;

public sealed class HttpPipelineTests
{
    [Fact]
    public async Task CfBenchmarksRetry_ReacquiresRateLimitAndSignsEveryAttempt()
    {
        var rateLimiter = new RecordingRateLimiter();
        var signer = new RecordingSigner();
        var terminalHandler = new RetryThenSuccessHandler();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKalshiClient(options =>
        {
            options.ApiKey = "test-api-key";
            options.ApiSecret = "test-api-secret";
            options.Environment = KalshiEnvironment.Demo;
        });
        services.AddSingleton<IRateLimiter>(rateLimiter);
        services.AddSingleton<IKalshiRequestSigner>(signer);
        services.AddHttpClient(ServiceCollectionExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => terminalHandler);

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IKalshiClient>();

        var result = await client.CfBenchmarks!.GetCurrentAsync("values");

        result.Data.GetProperty("value").GetString().Should().Be("68000.12");
        terminalHandler.AttemptCount.Should().Be(2);
        rateLimiter.Requests.Should().HaveCount(2)
            .And.OnlyContain(request => !request.IsWrite && request.TokenCost == 50);
        signer.CallCount.Should().Be(2);
    }

    private sealed class RecordingRateLimiter : IRateLimiter
    {
        private readonly List<RateLimitRequest> _requests = [];

        public IReadOnlyList<RateLimitRequest> Requests => _requests;

        public bool IsThrottling => false;

        public ValueTask WaitAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask WaitAsync(RateLimitRequest request, CancellationToken cancellationToken = default)
        {
            _requests.Add(request);
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingSigner : IKalshiRequestSigner
    {
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public void Sign(HttpRequestMessage request, ReadOnlySpan<byte> body, DateTimeOffset timestamp) =>
            Interlocked.Increment(ref _callCount);
    }

    private sealed class RetryThenSuccessHandler : HttpMessageHandler
    {
        private int _attemptCount;

        public int AttemptCount => Volatile.Read(ref _attemptCount);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref _attemptCount);
            var response = attempt == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":{\"value\":\"68000.12\"}}")
                };

            return Task.FromResult(response);
        }
    }
}
