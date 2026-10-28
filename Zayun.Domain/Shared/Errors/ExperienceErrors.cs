namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for experience areas. Nothing here is thrown.</summary>
public static class ExperienceErrors
{
    /// <summary>No experience area with this Id.</summary>
    public static readonly Error NotFound = Error.NotFound(
        "Experience.NotFound", "That experience area wasn't found.");
}
