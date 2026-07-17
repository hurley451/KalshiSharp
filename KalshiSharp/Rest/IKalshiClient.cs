using KalshiSharp.Rest.Events;
using KalshiSharp.Rest.Exchange;
using KalshiSharp.Rest.Markets;
using KalshiSharp.Rest.Orders;
using KalshiSharp.Rest.Portfolio;
using KalshiSharp.Rest.Users;

namespace KalshiSharp.Rest;

/// <summary>
/// Root client interface for the Kalshi API, providing access to all sub-clients.
/// </summary>
public interface IKalshiClient : IDisposable
{
    /// <summary>
    /// Gets the exchange client for status and schedule endpoints.
    /// </summary>
    IExchangeClient Exchange { get; }

    /// <summary>
    /// Gets the markets client for market data endpoints.
    /// </summary>
    IMarketClient Markets { get; }

    /// <summary>
    /// Gets the events client for event endpoints.
    /// </summary>
    IEventClient Events { get; }

    /// <summary>
    /// Gets the orders client for order management endpoints.
    /// </summary>
    IOrderClient Orders { get; }

    /// <summary>
    /// Gets the portfolio client for balance, positions, and fills endpoints.
    /// </summary>
    IPortfolioClient Portfolio { get; }

    /// <summary>
    /// Gets the users client for user profile endpoints.
    /// </summary>
    IUserClient Users { get; }
}
