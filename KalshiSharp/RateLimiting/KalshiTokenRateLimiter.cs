using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using KalshiSharp.Configuration;

namespace KalshiSharp.RateLimiting;

/// <summary>Separate read, unscoped-write, and shard-local write token buckets.</summary>
public sealed class KalshiTokenRateLimiter : IRateLimiter
{
    private readonly Bucket _read;
    private readonly Bucket _unscopedWrite;
    private readonly ConcurrentDictionary<int, Bucket> _shardWrites = new();
    private readonly KalshiRateLimitOptions _options;
    private bool _disposed;

    /// <summary>Creates a limiter from the configured token budgets.</summary>
    public KalshiTokenRateLimiter(KalshiRateLimitOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        Validate(options);
        _read = CreateBucket(options.ReadTokensPerSecond, options.ReadTokenLimit, options.QueueLimit);
        _unscopedWrite = CreateBucket(options.WriteTokensPerSecond, options.WriteTokenLimit, options.QueueLimit);
    }

    /// <inheritdoc />
    public bool IsThrottling => _read.IsThrottling || _unscopedWrite.IsThrottling || _shardWrites.Values.Any(x => x.IsThrottling);

    /// <inheritdoc />
    public ValueTask WaitAsync(CancellationToken cancellationToken = default) =>
        WaitAsync(new RateLimitRequest { IsWrite = false, TokenCost = 1 }, cancellationToken);

    /// <inheritdoc />
    public async ValueTask WaitAsync(RateLimitRequest request, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.TokenCost);

        var bucket = request.IsWrite
            ? request is { IsBatch: false, ExchangeIndex: > 0 }
                ? _shardWrites.GetOrAdd(request.ExchangeIndex.Value, _ =>
                    CreateBucket(_options.WriteTokensPerSecond, _options.WriteTokenLimit, _options.QueueLimit))
                : _unscopedWrite
            : _read;

        await bucket.AcquireAsync(request.TokenCost, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _read.Dispose();
        _unscopedWrite.Dispose();
        foreach (var bucket in _shardWrites.Values) bucket.Dispose();
        _disposed = true;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private static void Validate(KalshiRateLimitOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.ReadTokensPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.ReadTokenLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.WriteTokensPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.WriteTokenLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.QueueLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.DefaultTokenCost);
    }

    private static Bucket CreateBucket(int tokensPerSecond, int tokenLimit, int queueLimit) =>
        new(tokensPerSecond, tokenLimit, queueLimit);

    private sealed class Bucket : IDisposable
    {
        private readonly System.Threading.RateLimiting.TokenBucketRateLimiter _limiter;
        private readonly int _tokenLimit;
        private readonly int _queueLimit;
        private readonly SemaphoreSlim _acquisitionGate = new(1, 1);

        public Bucket(int tokensPerSecond, int tokenLimit, int queueLimit)
        {
            _tokenLimit = tokenLimit;
            _queueLimit = queueLimit;
            _limiter = new System.Threading.RateLimiting.TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = tokenLimit,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = tokensPerSecond,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = queueLimit,
                AutoReplenishment = true
            });
        }

        public bool IsThrottling => _limiter.GetStatistics()?.CurrentAvailablePermits < Math.Min(10, _tokenLimit);

        public async ValueTask AcquireAsync(int cost, CancellationToken cancellationToken)
        {
            await _acquisitionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var remaining = cost;
                while (remaining > 0)
                {
                    var permits = Math.Min(remaining, Math.Min(_tokenLimit, _queueLimit));
                    using var lease = await _limiter.AcquireAsync(permits, cancellationToken).ConfigureAwait(false);
                    if (!lease.IsAcquired)
                    {
                        throw new InvalidOperationException("Failed to acquire the configured Kalshi token budget.");
                    }
                    remaining -= permits;
                }
            }
            finally
            {
                _acquisitionGate.Release();
            }
        }

        public void Dispose()
        {
            _limiter.Dispose();
            _acquisitionGate.Dispose();
        }
    }
}
