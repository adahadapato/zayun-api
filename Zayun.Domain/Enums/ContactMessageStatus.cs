namespace Zayun.Domain.Enums;

/// <summary>Where a Contact message is in the inbox.</summary>
public enum ContactMessageStatus
{
    /// <summary>Not looked at yet (counted on the admin dashboard).</summary>
    New = 1,

    /// <summary>Answered by email; may be waiting for a reply back.</summary>
    Replied = 2,

    /// <summary>Done: nothing more to do.</summary>
    Closed = 3
}
