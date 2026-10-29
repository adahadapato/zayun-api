namespace Zayun.Application.Common.Caching;

/// <summary>
/// Every cache tag in one place. A public query is cached under its tag with <c>[Cacheable]</c>; every admin command
/// that changes the same data clears it with <c>[InvalidatesCache]</c>, so the website never shows stale content for
/// long.
/// </summary>
/// <remarks><b>Never cached:</b> admin screens and the Contact inbox (they must always be current).</remarks>
public static class CacheTags
{
    /// <summary>GET /api/projects and GET /api/projects/{slug}.</summary>
    public const string Projects = "projects";

    /// <summary>GET /api/publications.</summary>
    public const string Publications = "publications";

    /// <summary>GET /api/presentations.</summary>
    public const string Presentations = "presentations";

    /// <summary>GET /api/experience.</summary>
    public const string Experience = "experience";

    /// <summary>GET /api/skills (skill groups and core expertise together).</summary>
    public const string Skills = "skills";

    /// <summary>GET /api/awards.</summary>
    public const string Awards = "awards";

    /// <summary>GET /api/profiles.</summary>
    public const string Profiles = "profiles";

    /// <summary>GET /api/site-images (the owner's photo and other named pictures).</summary>
    public const string SiteImages = "site-images";

    /// <summary>GET /api/personal-details (only what's switched on for the website).</summary>
    public const string PersonalDetails = "personal-details";
}
