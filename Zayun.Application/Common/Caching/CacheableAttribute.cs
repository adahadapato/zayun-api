namespace Zayun.Application.Common.Caching;

/// <summary>
/// Put this on a QUERY to cache its successful result. Failures are never cached.
///
/// Example:
///   [Cacheable(CacheTags.Projects, Minutes = 60)]
///   public sealed record GetProjectsQuery() : IRequest<Result<IReadOnlyList<ProjectDto>>>;
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CacheableAttribute : Attribute
{
    public CacheableAttribute(params string[] tags)
    {
        if (tags is null || tags.Length == 0)
            throw new ArgumentException("A cached query needs at least one tag so it can be cleared.", nameof(tags));

        Tags = tags;
    }

    /// <summary>Tags used to clear this entry later (see InvalidatesCacheAttribute).</summary>
    public string[] Tags { get; }

    /// <summary>How long the result stays cached. Default 5 minutes.</summary>
    public int Minutes { get; init; } = 5;

    /// <summary>
    /// true = each signed-in user gets their own cached copy.
    /// Use this for anything that depends on WHO is asking.
    /// </summary>
    public bool PerUser { get; init; }
}