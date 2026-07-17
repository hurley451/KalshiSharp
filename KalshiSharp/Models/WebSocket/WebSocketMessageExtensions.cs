using System;
using System.Globalization;

namespace KalshiSharp.Models.WebSocket;

/// <summary>
/// Extension methods for WebSocket message types to convert dollar-based fields to cents.
/// </summary>
public static class WebSocketMessageExtensions
{
    #region TickerUpdate Extensions

    /// <summary>
    /// Gets the last traded price in cents from price_dollars field.
    /// </summary>
    public static int? Price(this TickerUpdate.MessageBody message)
    {
        return ParseDollarsToCents(message.PriceDollars);
    }

    /// <summary>
    /// Gets the YES bid price in cents from yes_bid_dollars field.
    /// </summary>
    public static int? YesBid(this TickerUpdate.MessageBody message)
    {
        return ParseDollarsToCents(message.YesBidDollars);
    }

    /// <summary>
    /// Gets the YES ask price in cents from yes_ask_dollars field.
    /// </summary>
    public static int? YesAsk(this TickerUpdate.MessageBody message)
    {
        return ParseDollarsToCents(message.YesAskDollars);
    }

    /// <summary>
    /// Gets the NO bid price in cents (calculated from YES ask).
    /// </summary>
    public static int? NoBid(this TickerUpdate.MessageBody message)
    {
        var yesAsk = message.YesAsk();
        return yesAsk.HasValue ? 100 - yesAsk.Value : null;
    }

    /// <summary>
    /// Gets the NO ask price in cents (calculated from YES bid).
    /// </summary>
    public static int? NoAsk(this TickerUpdate.MessageBody message)
    {
        var yesBid = message.YesBid();
        return yesBid.HasValue ? 100 - yesBid.Value : null;
    }

    /// <summary>
    /// Gets the volume from volume_fp field.
    /// </summary>
    public static decimal? Volume(this TickerUpdate.MessageBody message)
    {
        return ParseFixedPoint(message.VolumeFp);
    }

    /// <summary>
    /// Gets the open interest from open_interest_fp field.
    /// </summary>
    public static decimal? OpenInterest(this TickerUpdate.MessageBody message)
    {
        return ParseFixedPoint(message.OpenInterestFp);
    }

    #endregion

    #region OrderBookUpdate Extensions

    /// <summary>
    /// Gets the price level in cents from price_dollars field.
    /// </summary>
    public static int? Price(this OrderBookUpdate.MessageBody message)
    {
        return ParseDollarsToCents(message.PriceDollars);
    }

    /// <summary>
    /// Gets the delta (change in quantity) from delta_fp field.
    /// </summary>
    public static decimal? Delta(this OrderBookUpdate.MessageBody message)
    {
        return ParseFixedPoint(message.DeltaFp);
    }

    #endregion

    #region TradeUpdate Extensions

    /// <summary>
    /// Gets the YES price in cents from yes_price_dollars field.
    /// </summary>
    public static int? YesPrice(this TradeUpdate.MessageBody message)
    {
        return ParseDollarsToCents(message.YesPriceDollars);
    }

    /// <summary>
    /// Gets the NO price in cents from no_price_dollars field.
    /// </summary>
    public static int? NoPrice(this TradeUpdate.MessageBody message)
    {
        return ParseDollarsToCents(message.NoPriceDollars);
    }

    /// <summary>
    /// Gets the count (number of contracts) from count_fp field.
    /// </summary>
    public static decimal? Count(this TradeUpdate.MessageBody message)
    {
        return ParseFixedPoint(message.CountFp);
    }

    #endregion

    #region FillUpdate Extensions

    /// <summary>
    /// Gets the YES price in cents from yes_price_dollars field.
    /// </summary>
    public static int? YesPrice(this FillUpdate.MessageBody message)
    {
        return ParseDollarsToCents(message.YesPriceDollars);
    }

    /// <summary>
    /// Gets the NO price in cents (calculated from YES price).
    /// </summary>
    public static int? NoPrice(this FillUpdate.MessageBody message)
    {
        var yesPrice = message.YesPrice();
        return yesPrice.HasValue ? 100 - yesPrice.Value : null;
    }

    /// <summary>
    /// Gets the fill price based on the order side.
    /// </summary>
    public static int? FillPrice(this FillUpdate.MessageBody message)
    {
        return message.Side == Enums.OrderSide.Yes ? message.YesPrice() : message.NoPrice();
    }

    /// <summary>
    /// Gets the count (number of contracts filled) from count_fp field.
    /// </summary>
    public static decimal? Count(this FillUpdate.MessageBody message)
    {
        return ParseFixedPoint(message.CountFp);
    }

    /// <summary>
    /// Gets the post-fill position from post_position_fp field.
    /// </summary>
    public static decimal? PostPosition(this FillUpdate.MessageBody message)
    {
        return ParseFixedPoint(message.PostPositionFp);
    }

    #endregion

    #region MarketPositionUpdate Extensions

    /// <summary>
    /// Gets the position from position_fp field.
    /// </summary>
    public static decimal? Position(this MarketPositionUpdate.MessageBody message)
    {
        return ParseFixedPoint(message.PositionFp);
    }

    /// <summary>
    /// Gets the position cost in dollars from position_cost_dollars field.
    /// </summary>
    public static decimal? PositionCostDollars(this MarketPositionUpdate.MessageBody message)
    {
        return ParseDollars(message.PositionCostDollars);
    }

    /// <summary>
    /// Gets the realized PnL in dollars from realized_pnl_dollars field.
    /// </summary>
    public static decimal? RealizedPnlDollars(this MarketPositionUpdate.MessageBody message)
    {
        return ParseDollars(message.RealizedPnlDollars);
    }

    /// <summary>
    /// Gets the fees paid in dollars from fees_paid_dollars field.
    /// </summary>
    public static decimal? FeesPaidDollars(this MarketPositionUpdate.MessageBody message)
    {
        return ParseDollars(message.FeesPaidDollars);
    }

    /// <summary>
    /// Gets the position fee cost in dollars from position_fee_cost_dollars field.
    /// </summary>
    public static decimal? PositionFeeCostDollars(this MarketPositionUpdate.MessageBody message)
    {
        return ParseDollars(message.PositionFeeCostDollars);
    }

    /// <summary>
    /// Gets the volume from volume_fp field.
    /// </summary>
    public static decimal? Volume(this MarketPositionUpdate.MessageBody message)
    {
        return ParseFixedPoint(message.VolumeFp);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Parses a dollar string value (e.g., "0.56") to cents (e.g., 56).
    /// </summary>
    private static int? ParseDollarsToCents(string? dollarString)
    {
        if (string.IsNullOrWhiteSpace(dollarString))
        {
            return null;
        }

        if (decimal.TryParse(dollarString, NumberStyles.Number, CultureInfo.InvariantCulture, out var dollars))
        {
            return (int)Math.Round(dollars * 100);
        }

        return null;
    }

    /// <summary>
    /// Parses a dollar string value to decimal.
    /// </summary>
    private static decimal? ParseDollars(string? dollarString)
    {
        if (string.IsNullOrWhiteSpace(dollarString))
        {
            return null;
        }

        if (decimal.TryParse(dollarString, NumberStyles.Number, CultureInfo.InvariantCulture, out var dollars))
        {
            return dollars;
        }

        return null;
    }

    /// <summary>
    /// Parses a fixed-point string value to decimal.
    /// </summary>
    private static decimal? ParseFixedPoint(string? fpString)
    {
        if (string.IsNullOrWhiteSpace(fpString))
        {
            return null;
        }

        if (decimal.TryParse(fpString, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return null;
    }

    #endregion
}
