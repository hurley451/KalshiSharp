namespace KalshiSharp.Models.Enums;

/// <summary>
/// Represents how long an order remains active before expiration.
/// </summary>
/// <remarks>
/// Serialized as lowercase strings: "gtc", "ioc", "fok".
/// </remarks>
public enum TimeInForce
{
    /// <summary>
    /// Good Till Cancelled - order remains active until explicitly cancelled.
    /// </summary>
    Gtc,

    /// <summary>
    /// Immediate Or Cancel - execute immediately and cancel any unfilled portion.
    /// </summary>
    Ioc,

    /// <summary>
    /// Fill Or Kill - execute entire order immediately or cancel completely.
    /// </summary>
    Fok
}
