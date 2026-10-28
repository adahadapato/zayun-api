namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for publications. Nothing here is thrown.</summary>
public static class PublicationErrors
{
    /// <summary>No publication with this Id.</summary>
    public static readonly Error NotFound = Error.NotFound(
        "Publication.NotFound", "That publication wasn't found.");
}
