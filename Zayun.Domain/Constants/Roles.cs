namespace Zayun.Domain.Constants;

/// <summary>
/// Identity role names. Seeded at start-up; use these constants instead of typing strings.
/// </summary>
/// <remarks>Zayun has no public sign-up: the only accounts are the site's administrators.</remarks>
public static class Roles
{
    /// <summary>Everything: edit the site's content and read the Contact inbox.</summary>
    public const string Admin = "Admin";

    /// <summary>All roles, for seeding.</summary>
    public static readonly IReadOnlyList<string> All = [Admin];
}
