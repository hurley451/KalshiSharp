namespace KalshiSharp.Models.Enums;

/// <summary>
/// Represents the current status of an event.
/// </summary>
/// <remarks>
/// Serialized as lowercase strings: "open", "closed", "settled".
/// </remarks>
public enum EventStatus
{
    /// <summary>
    /// Event is open and currently active.
    /// </summary>
    Open,

    /// <summary>
    /// Event is closed and no longer active.
    /// </summary>
    Closed,

    /// <summary>
    /// Settlement has occured for the event.
    /// </summary>
    Settled
}
