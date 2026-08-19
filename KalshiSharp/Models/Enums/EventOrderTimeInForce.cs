namespace KalshiSharp.Models.Enums;

/// <summary>
/// Time-in-force values accepted by the V2 event-order API.
/// </summary>
public enum EventOrderTimeInForce
{
    /// <summary>Remain active until canceled or expired.</summary>
    GoodTillCanceled,

    /// <summary>Fill immediately and cancel any remainder.</summary>
    ImmediateOrCancel,

    /// <summary>Fill completely immediately or cancel.</summary>
    FillOrKill
}
