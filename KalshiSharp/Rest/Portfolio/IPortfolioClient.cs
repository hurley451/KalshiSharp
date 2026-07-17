using KalshiSharp.Models.Enums;
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

    /// <summary>
    /// Lists fills (trade executions) with optional filtering and pagination.
    /// </summary>
    /// <param name="cursor">Cursor for pagination.</param>
    /// <param name="limit">Maximum number of fills to return.</param>
    /// <param name="ticker">Optional market ticker filter.</param>
    /// <param name="orderId">Optional order ID filter.</param>
    /// <param name="minTs">Optional minimum fill time filter (Unix timestamp).</param>
    /// <param name="maxTs">Optional maximum fill time filter (Unix timestamp).</param>
    /// <param name="side">Optional side filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of fills.</returns>
    Task<FillsResponse> ListFillsAsync(
        string? cursor = null,
        int? limit = null,
        string? ticker = null,
        string? orderId = null,
        DateTimeOffset? minTs = null,
        DateTimeOffset? maxTs = null,
        OrderSide? side = null,
        CancellationToken cancellationToken = default);
}
