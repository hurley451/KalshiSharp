using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Http;
using KalshiSharp.Observability;
using KalshiSharp.RateLimiting;
using KalshiSharp.Rest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace KalshiSharp.DependencyInjection;

/// <summary>
/// Extension methods for configuring Kalshi client services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The named HttpClient name used for Kalshi API requests.
    /// </summary>
    public const string HttpClientName = "KalshiApi";

    /// <summary>
    /// Adds Kalshi client services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKalshiClient(
        this IServiceCollection services,
        Action<KalshiClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Configure options
        services.Configure(configure);

        // Register core services
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IKalshiRequestSigner, RsaPssRequestSigner>();
        services.AddSingleton<IRateLimiter>(sp =>
            new KalshiTokenRateLimiter(sp.GetRequiredService<IOptions<KalshiClientOptions>>().Value.RateLimits));
        services.AddSingleton<KalshiClientMetrics>();

        // Register delegating handlers
        services.AddTransient<SigningDelegatingHandler>();
        services.AddTransient(sp =>
        {
            var rateLimiter = sp.GetRequiredService<IRateLimiter>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RateLimitingDelegatingHandler>>();
            var options = sp.GetRequiredService<IOptions<KalshiClientOptions>>();
            return new RateLimitingDelegatingHandler(
                rateLimiter,
                logger,
                options.Value.EnableRateLimiting,
                options.Value.RateLimits.DefaultTokenCost);
        });

        // Configure HttpClient with resilience pipeline
        services.AddHttpClient<IKalshiHttpClient, KalshiHttpClient>(HttpClientName)
            .AddHttpMessageHandler<RateLimitingDelegatingHandler>()
            .AddHttpMessageHandler<SigningDelegatingHandler>()
            .AddStandardResilienceHandler(options =>
            {
                // Retry policy: max 3 retries with exponential backoff and jitter
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Result?.StatusCode is
                        System.Net.HttpStatusCode.TooManyRequests or
                        System.Net.HttpStatusCode.InternalServerError or
                        System.Net.HttpStatusCode.BadGateway or
                        System.Net.HttpStatusCode.ServiceUnavailable or
                        System.Net.HttpStatusCode.GatewayTimeout);

                // Circuit breaker: open after 5 failures in 60 seconds
                // Note: SamplingDuration must be at least 2x AttemptTimeout
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Result?.StatusCode is >= System.Net.HttpStatusCode.InternalServerError);

                // Attempt timeout: 30 seconds per attempt
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);

                // Total request timeout: 2 minutes
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
            });

        // Register the Kalshi client
        services.AddSingleton<IKalshiClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IKalshiHttpClient>();
            return new KalshiClient(httpClient);
        });

        return services;
    }

    /// <summary>
    /// Adds Kalshi client services with pre-configured options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="apiSecret">The API secret.</param>
    /// <param name="environment">The target environment.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKalshiClient(
        this IServiceCollection services,
        string apiKey,
        string apiSecret,
        KalshiEnvironment environment = KalshiEnvironment.Production)
    {
        return services.AddKalshiClient(options =>
        {
            options.ApiKey = apiKey;
            options.ApiSecret = apiSecret;
            options.Environment = environment;
        });
    }
}
