namespace Zayun.Domain.Constants;

/// <summary>
/// Authorization policy names. Each policy is registered in Presentation; the comment says which roles pass it.
/// </summary>
public static class Policies
{
    /// <summary>Admin only.</summary>
    public const string RequireAdmin = "RequireAdmin";
}
