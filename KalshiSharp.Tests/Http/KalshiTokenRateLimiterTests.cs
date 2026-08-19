using FluentAssertions;
using KalshiSharp.Configuration;
using KalshiSharp.Http;
using KalshiSharp.RateLimiting;
using Xunit;

namespace KalshiSharp.Tests.Http;

public sealed class KalshiTokenRateLimiterTests
{
    [Fact]
    public void Defaults_MatchBasicTierWriteBucket()
    {
        var options = new KalshiRateLimitOptions();

        options.WriteTokensPerSecond.Should().Be(100);
        options.WriteTokenLimit.Should().Be(100);
    }

    [Fact]
    public async Task ReadAndWriteBuckets_AreIndependent()
    {
        await using var limiter = new KalshiTokenRateLimiter(new KalshiRateLimitOptions
        {
            ReadTokensPerSecond = 1,
            ReadTokenLimit = 1,
            WriteTokensPerSecond = 1,
            WriteTokenLimit = 1,
            QueueLimit = 5
        });

        await limiter.WaitAsync(new RateLimitRequest { IsWrite = false, TokenCost = 1 });
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await limiter.WaitAsync(new RateLimitRequest { IsWrite = true, TokenCost = 1 });

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task ShardWriteBucket_IsIndependentFromUnscopedWrites()
    {
        await using var limiter = new KalshiTokenRateLimiter(new KalshiRateLimitOptions
        {
            ReadTokensPerSecond = 1,
            ReadTokenLimit = 1,
            WriteTokensPerSecond = 1,
            WriteTokenLimit = 1,
            QueueLimit = 5
        });

        await limiter.WaitAsync(new RateLimitRequest { IsWrite = true, TokenCost = 1 });
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await limiter.WaitAsync(new RateLimitRequest { IsWrite = true, TokenCost = 1, ExchangeIndex = 2 });

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task BatchCost_LargerThanQueueCapacity_WaitsInChunks()
    {
        await using var limiter = new KalshiTokenRateLimiter(new KalshiRateLimitOptions
        {
            ReadTokensPerSecond = 100,
            ReadTokenLimit = 100,
            WriteTokensPerSecond = 200,
            WriteTokenLimit = 200,
            QueueLimit = 100
        });

        await limiter.WaitAsync(new RateLimitRequest { IsWrite = true, TokenCost = 150 });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));

        var action = async () => await limiter.WaitAsync(
            new RateLimitRequest { IsWrite = true, IsBatch = true, TokenCost = 250 },
            timeout.Token);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ClassifyAsync_UsesCurrentOrderCostsAndShardRules()
    {
        var cancel = new HttpRequestMessage(HttpMethod.Delete,
            "https://example.test/trade-api/v2/portfolio/events/orders/order-1?exchange_index=3");
        var createBatch = new HttpRequestMessage(HttpMethod.Post,
            "https://example.test/trade-api/v2/portfolio/events/orders/batched")
        {
            Content = new StringContent("""{"orders":[{"exchange_index":1},{"exchange_index":2}]}""")
        };
        var legacyCreate = new HttpRequestMessage(HttpMethod.Post,
            "https://example.test/trade-api/v2/portfolio/orders")
        {
            Content = new StringContent("{}")
        };

        var cancelResult = await RateLimitingDelegatingHandler.ClassifyAsync(cancel, 10, default);
        var batchResult = await RateLimitingDelegatingHandler.ClassifyAsync(createBatch, 10, default);
        var legacyResult = await RateLimitingDelegatingHandler.ClassifyAsync(legacyCreate, 10, default);

        cancelResult.TokenCost.Should().Be(2);
        cancelResult.ExchangeIndex.Should().Be(3);
        batchResult.TokenCost.Should().Be(20);
        batchResult.IsBatch.Should().BeTrue();
        batchResult.ExchangeIndex.Should().BeNull();
        legacyResult.TokenCost.Should().Be(100);
    }

    [Fact]
    public async Task ClassifyAsync_ReadsUseDefaultCost()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/trade-api/v2/markets");

        var result = await RateLimitingDelegatingHandler.ClassifyAsync(request, 17, default);

        result.IsWrite.Should().BeFalse();
        result.TokenCost.Should().Be(17);
    }
}
