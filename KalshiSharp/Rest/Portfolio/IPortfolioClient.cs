using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;

namespace KalshiSharp.Rest.Portfolio;

/// <summary>
/// Client for portfolio operations including balance, positions, and fills.
/// </summary>
public interface IPortfolioClient
{
    /// <summary>
    /// Retrieves the user's account balance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account balance details.</returns>
    Task<BalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves an explicitly scoped balance.</summary>
    Task<BalanceResponse> GetBalanceAsync(BalanceQuery query, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This portfolio client does not support scoped balances.");

    /// <summary>
    /// Lists positions with optional filtering and pagination.
    /// </summary>
    /// <param name="cursor">Cursor for pagination.</param>
    /// <param name="limit">Maximum number of positions to return.</param>
    /// <param name="ticker">Optional market ticker filter.</param>
    /// <param name="eventTicker">Optional event ticker filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of positions.</returns>
    Task<PositionsResponse> ListPositionsAsync(
        string? cursor = null,
        int? limit = null,
        string? ticker = null,
        string? eventTicker = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lists positions using the current query contract.</summary>
    Task<PositionsResponse> ListPositionsAsync(PositionQuery query, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This portfolio client does not support the current position query contract.");

    /// <summary>
    /// Lists fills (trade executions) with optional filtering and pagination.
    /// </summary>
    /// <param name="cursor">Cursor for pagination.</param>
    /// <param name="limit">Maximum number of fills to return.</param>
    /// <param name="ticker">Optional market ticker filter.</param>
    /// <param name="orderId">Optional order ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of fills.</returns>
    Task<FillsResponse> ListFillsAsync(
        string? cursor = null,
        int? limit = null,
        string? ticker = null,
        string? orderId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lists fills using the current query contract.</summary>
    Task<FillsResponse> ListFillsAsync(FillQuery query, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This portfolio client does not support the current fill query contract.");
}
