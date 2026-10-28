namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for presentations. Nothing here is thrown.</summary>
public static class PresentationErrors
{
    /// <summary>No presentation with this Id.</summary>
    public static readonly Error NotFound = Error.NotFound(
        "Presentation.NotFound", "That presentation wasn't found.");

    /// <summary>Asked to remove a picture that isn't there.</summary>
    public static readonly Error NoImage = Error.NotFound(
        "Presentation.NoImage", "This presentation has no picture to remove.");
}
