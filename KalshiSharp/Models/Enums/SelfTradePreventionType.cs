namespace KalshiSharp.Models.Enums;

/// <summary>
/// Represents the self-trade prevention strategy for a V2 order.
/// </summary>
/// <remarks>
/// Serialized as snake_case strings: "taker_at_cross", "maker".
/// </remarks>
public enum SelfTradePreventionType
{
    /// <summary>
    /// Cancels the incoming taker order when it would trade against another order from the same user.
    /// Any partial fills already matched are executed.
    /// </summary>
    TakerAtCross,

    /// <summary>
    /// Cancels the resting maker order and continues matching.
    /// </summary>
    Maker
}
