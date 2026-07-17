using System.Net.Http.Headers;
using KalshiSharp.Auth;
using KalshiSharp.Configuration;
using KalshiSharp.Http;
using KalshiSharp.Rest.Events;
using KalshiSharp.Rest.Exchange;
using KalshiSharp.Rest.Markets;
using KalshiSharp.Rest.Orders;
using KalshiSharp.Rest.Portfolio;
using KalshiSharp.Rest.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KalshiSharp.Rest;

/// <summary>
/// Root client implementation for the Kalshi API.
/// </summary>
public sealed class KalshiClient : IKalshiClient
{
    private readonly IExchangeClient _exchange;
    private readonly IMarketClient _markets;
    private readonly IEventClient _events;
    private readonly IOrderClient _orders;
    private readonly IPortfolioClient _portfolio;
    private readonly IUserClient _users;

    // Resources we own and must dispose (only set when using direct instantiation)
    private readonly HttpClient? _ownedHttpClient;
    private readonly RsaPssRequestSigner? _ownedSigner;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KalshiClient"/> class with the specified options.
    /// This is the recommended constructor for simple usage without dependency injection.
    /// </summary>
    /// <param name="options">The client options containing API credentials and configuration.</param>
    /// <param name="logger">Optional logger. If not provided, logging is disabled.</param>
    /// <example>
    /// <code>
    /// var client = new KalshiClient(new KalshiClientOptions
    /// {
    ///     ApiKey = "your-api-key-id",
    ///     ApiSecret = "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----",
    ///     Environment = KalshiEnvironment.Production
    /// });
    ///
    /// var status = await client.Exchange.GetStatusAsync();
    /// </code>
    /// </example>
    public KalshiClient(KalshiClientOptions options, ILogger<KalshiClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Create the signer
        _ownedSigner = new RsaPssRequestSigner(options.ApiKey, options.ApiSecret);

        // Create HTTP client with signing handler
        var signingHandler = new SimpleSigningHandler(_ownedSigner, new SystemClock())
        {
            InnerHandler = new HttpClientHandler()
        };

        _ownedHttpClient = new HttpClient(signingHandler)
        {
            BaseAddress = options.GetEffectiveBaseUri(),
            Timeout = options.Timeout
        };
        _ownedHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Create the Kalshi HTTP client wrapper
        var httpClientLogger = logger as ILogger<KalshiHttpClient>
            ?? NullLogger<KalshiHttpClient>.Instance;
        var kalshiHttpClient = new KalshiHttpClient(
            _ownedHttpClient,
            Options.Create(options),
            httpClientLogger);

        // Create sub-clients
        _exchange = new ExchangeClient(kalshiHttpClient);
        _markets = new MarketClient(kalshiHttpClient);
        _events = new EventClient(kalshiHttpClient);
        _orders = new OrderClient(kalshiHttpClient);
        _portfolio = new PortfolioClient(kalshiHttpClient);
        _users = new UserClient(kalshiHttpClient);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KalshiClient"/> class.
    /// Used when the HTTP client is provided externally (e.g., via dependency injection).
    /// </summary>
    /// <param name="httpClient">The HTTP client for API requests.</param>
    public KalshiClient(IKalshiHttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _exchange = new ExchangeClient(httpClient);
        _markets = new MarketClient(httpClient);
        _events = new EventClient(httpClient);
        _orders = new OrderClient(httpClient);
        _portfolio = new PortfolioClient(httpClient);
        _users = new UserClient(httpClient);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KalshiClient"/> class with explicit sub-clients.
    /// Used for DI scenarios where sub-clients are injected directly.
    /// </summary>
    /// <param name="exchange">The exchange client.</param>
    /// <param name="markets">The markets client.</param>
    /// <param name="events">The events client.</param>
    /// <param name="orders">The orders client.</param>
    /// <param name="portfolio">The portfolio client.</param>
    /// <param name="users">The users client.</param>
    public KalshiClient(
        IExchangeClient exchange,
        IMarketClient markets,
        IEventClient events,
        IOrderClient orders,
        IPortfolioClient portfolio,
        IUserClient users)
    {
        _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
        _markets = markets ?? throw new ArgumentNullException(nameof(markets));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _orders = orders ?? throw new ArgumentNullException(nameof(orders));
        _portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    /// <inheritdoc />
    public IExchangeClient Exchange
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _exchange;
        }
    }

    /// <inheritdoc />
    public IMarketClient Markets
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _markets;
        }
    }

    /// <inheritdoc />
    public IEventClient Events
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _events;
        }
    }

    /// <inheritdoc />
    public IOrderClient Orders
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _orders;
        }
    }

    /// <inheritdoc />
    public IPortfolioClient Portfolio
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _portfolio;
        }
    }

    /// <inheritdoc />
    public IUserClient Users
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _users;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Dispose resources we own (from direct instantiation)
        _ownedHttpClient?.Dispose();
        _ownedSigner?.Dispose();
    }
}

/// <summary>
/// Simple signing handler for direct instantiation scenarios (no DI).
/// </summary>
internal sealed class SimpleSigningHandler : DelegatingHandler
{
    private readonly RsaPssRequestSigner _signer;
    private readonly ISystemClock _clock;

    public SimpleSigningHandler(RsaPssRequestSigner signer, ISystemClock clock)
    {
        _signer = signer;
        _clock = clock;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        byte[] bodyBytes = [];

        if (request.Content is not null)
        {
            bodyBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }

        var timestamp = _clock.UtcNow;
        _signer.Sign(request, bodyBytes, timestamp);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
