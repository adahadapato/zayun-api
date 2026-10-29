using Zayun.Domain.Entities;
using Zayun.Domain.Shared.Results;

namespace Zayun.Domain.Interfaces;

/// <summary>
/// Loads and saves the shared tags. Implemented in Persistence. Built to stay fast with thousands of tags: nothing
/// here loads the whole list.
/// </summary>
public interface ITagRepository : IRepository<Tag>
{
    /// <summary>The tags found by any of these keys (their own key or an alias). Loaded for update.</summary>
    /// <param name="keys">Keys from <c>TagNames.Key</c>.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<IReadOnlyList<Tag>> FindByKeysAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default);

    /// <summary>One page of the admin list.</summary>
    /// <param name="search">Text in the name or an alias (any case, spaces and dots ignored); empty for all.</param>
    /// <param name="filter">All, used or unused.</param>
    /// <param name="sort">By name, or most used first.</param>
    /// <param name="page">Which page.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<PagedResult<Tag>> SearchAsync(string? search, TagFilter filter, TagSort sort, PageRequest page,
                                       CancellationToken cancellationToken = default);

    /// <summary>How many tags there are, and how many are used.</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<(int Total, int Used)> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The tag names worth showing the AI for a new name: every tag while the list is small, otherwise tags sharing a
    /// word with it plus the most used ones.
    /// </summary>
    /// <param name="name">The typed name.</param>
    /// <param name="max">Most names to return.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<IReadOnlyList<string>> GetShortlistAsync(string name, int max, CancellationToken cancellationToken = default);

    /// <summary>Tags whose stored usage is above zero (to recount them). Loaded for update.</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<IReadOnlyList<Tag>> GetUsedAsync(CancellationToken cancellationToken = default);
}

/// <summary>Which tags the admin list shows.</summary>
public enum TagFilter
{
    All = 1,
    Used = 2,
    Unused = 3
}

/// <summary>How the admin list is sorted.</summary>
public enum TagSort
{
    /// <summary>A to Z.</summary>
    Name = 1,

    /// <summary>Most used first, then A to Z.</summary>
    Usage = 2
}
