namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for awards. Nothing here is thrown.</summary>
public static class AwardErrors
{
    /// <summary>No award with this Id.</summary>
    public static readonly Error NotFound = Error.NotFound(
        "Award.NotFound", "That award wasn't found.");

    /// <summary>Asked to remove a picture that isn't there.</summary>
    public static readonly Error NoImage = Error.NotFound(
        "Award.NoImage", "This award has no picture to remove.");
}
