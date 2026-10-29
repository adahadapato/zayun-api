namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for the website's named pictures. Nothing here is thrown.</summary>
public static class SiteImageErrors
{
    /// <summary>The slot has no picture.</summary>
    public static readonly Error Empty = Error.NotFound("SiteImage.Empty", "There's no picture here yet.");
}
