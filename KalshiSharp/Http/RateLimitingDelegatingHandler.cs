using KalshiSharp.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KalshiSharp.Http;

/// <summary>
/// Delegating handler that applies client-side rate limiting before sending requests.
/// </summary>
public sealed partial class RateLimitingDelegatingHandler : DelegatingHandler
{
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitingDelegatingHandler> _logger;
    private readonly bool _enabled;
    private readonly int _defaultTokenCost;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingDelegatingHandler"/> class.
    /// </summary>
    /// <param name="rateLimiter">The rate limiter.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="enabled">Whether rate limiting is enabled.</param>
    public RateLimitingDelegatingHandler(
        IRateLimiter rateLimiter,
        ILogger<RateLimitingDelegatingHandler> logger,
        bool enabled = true)
        : this(rateLimiter, logger, enabled, 10)
    {
    }

    /// <summary>Initializes a handler with an explicit fallback token cost.</summary>
    /// <param name="rateLimiter">The rate limiter.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="enabled">Whether rate limiting is enabled.</param>
    /// <param name="defaultTokenCost">Fallback token cost for unclassified endpoints.</param>
    public RateLimitingDelegatingHandler(
        IRateLimiter rateLimiter,
        ILogger<RateLimitingDelegatingHandler> logger,
        bool enabled,
        int defaultTokenCost)
    {
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enabled = enabled;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultTokenCost);
        _defaultTokenCost = defaultTokenCost;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_enabled)
        {
            if (_rateLimiter.IsThrottling)
            {
                LogThrottling();
            }

            var classification = await ClassifyAsync(request, _defaultTokenCost, cancellationToken).ConfigureAwait(false);
            await _rateLimiter.WaitAsync(classification, cancellationToken).ConfigureAwait(false);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Rate limiter is throttling, waiting for permit")]
    private partial void LogThrottling();

    internal static async ValueTask<RateLimitRequest> ClassifyAsync(
        HttpRequestMessage request,
        int defaultTokenCost,
        CancellationToken cancellationToken)
    {
        var isWrite = request.Method != HttpMethod.Get && request.Method != HttpMethod.Head;
        if (!isWrite)
        {
            return new RateLimitRequest { IsWrite = false, TokenCost = defaultTokenCost };
        }

        var path = request.RequestUri is { IsAbsoluteUri: true } absolute
            ? absolute.AbsolutePath
            : request.RequestUri?.OriginalString.Split('?', 2)[0] ?? string.Empty;
        var isBatch = path.EndsWith("/batched", StringComparison.OrdinalIgnoreCase);
        var isV2Order = path.Contains("/portfolio/events/orders", StringComparison.OrdinalIgnoreCase);
        var isLegacyOrder = !isV2Order && path.Contains("/portfolio/orders", StringComparison.OrdinalIgnoreCase);
        var itemCount = 1;
        int? exchangeIndex = ParseQueryExchangeIndex(request.RequestUri);

        if (request.Content is not null && (isBatch || exchangeIndex is null))
        {
            var json = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(json))
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                if (isBatch && root.TryGetProperty("orders", out var orders) && orders.ValueKind == JsonValueKind.Array)
                {
                    itemCount = Math.Max(1, orders.GetArrayLength());
                }
                if (!isBatch && root.TryGetProperty("exchange_index", out var shard) && shard.TryGetInt32(out var parsed))
                {
                    exchangeIndex = parsed;
                }
            }
        }

        var perItemCost = isV2Order && request.Method == HttpMethod.Delete
            ? 2
            : isLegacyOrder
                ? request.Method == HttpMethod.Delete ? 20 : 100
                : defaultTokenCost;

        return new RateLimitRequest
        {
            IsWrite = true,
            TokenCost = checked(perItemCost * itemCount),
            IsBatch = isBatch,
            ExchangeIndex = !isBatch && exchangeIndex > 0 ? exchangeIndex : null
        };
    }

    private static int? ParseQueryExchangeIndex(Uri? uri)
    {
        var query = uri is { IsAbsoluteUri: true }
            ? uri.Query
            : uri?.OriginalString.Contains('?', StringComparison.Ordinal) == true
                ? $"?{uri.OriginalString.Split('?', 2)[1]}"
                : null;
        if (string.IsNullOrEmpty(query)) return null;
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && Uri.UnescapeDataString(parts[0]) == "exchange_index" &&
                int.TryParse(Uri.UnescapeDataString(parts[1]), System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
        }
        return null;
    }
}
