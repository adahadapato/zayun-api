namespace Zayun.Application.Common.Caching;

/// <summary>
/// Put this on a COMMAND. When the command succeeds, every cached entry
/// with these tags is removed, so the next read gets fresh data.
///
/// Example:
///   [InvalidatesCache(CacheTags.Projects)]
///   public sealed record UpdateProjectCommand(...) : IRequest<Result>;
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InvalidatesCacheAttribute : Attribute
{
    public InvalidatesCacheAttribute(params string[] tags)
    {
        if (tags is null || tags.Length == 0)
            throw new ArgumentException("Give at least one tag to clear.", nameof(tags));

        Tags = tags;
    }

    public string[] Tags { get; }
}