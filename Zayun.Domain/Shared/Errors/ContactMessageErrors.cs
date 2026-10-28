namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for Contact messages. Nothing here is thrown.</summary>
public static class ContactMessageErrors
{
    /// <summary>No message with this Id.</summary>
    public static readonly Error NotFound = Error.NotFound(
        "ContactMessage.NotFound", "This message isn't in the inbox any more.");
}
