namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for online profiles. Nothing here is thrown.</summary>
public static class ProfileErrors
{
    /// <summary>No profile with this Id.</summary>
    public static readonly Error NotFound = Error.NotFound("Profile.NotFound", "That profile wasn't found.");
}
