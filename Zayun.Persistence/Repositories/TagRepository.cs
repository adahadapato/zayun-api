using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.Shared.Text;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ITagRepository"/>.</summary>
/// <remarks>
/// <para>Lookups by key search <c>SearchKeys</c> with plain text matching (no JSON functions), so they work on any SQL
/// Server compatibility level and never load the whole list.</para>
/// <para>Tags found for update are TRACKED: renames, merges and usage counts change them in place.</para>
/// </remarks>
internal sealed class TagRepository(ZayunDbContext context) : Repository<Tag>(context), ITagRepository
{
    /// <summary>At this size or below, the AI sees every tag name.</summary>
    private const int SendAllToAiUpTo = 400;

    /// <summary>Keys looked up per query (each is one LIKE).</summary>
    private const int KeysPerQuery = 40;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Tag>> FindByKeysAsync(IReadOnlyCollection<string> keys,
                                                          CancellationToken cancellationToken = default)
    {
        var wanted = keys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
        var found = new Dictionary<Guid, Tag>();

        foreach (var chunk in wanted.Chunk(KeysPerQuery))
        {
            foreach (var tag in await Set.Where(AnyKey(chunk)).ToListAsync(cancellationToken))
                found.TryAdd(tag.Id, tag);
        }

        // The LIKE is a prefilter; keep only real matches
        return found.Values.Where(t => wanted.Any(t.Matches)).ToList();
    }

    /// <inheritdoc />
    public Task<PagedResult<Tag>> SearchAsync(string? search, TagFilter filter, TagSort sort, PageRequest page,
                                              CancellationToken cancellationToken = default)
    {
        IQueryable<Tag> query = Set.AsNoTracking();

        var key = TagNames.Key(search);
        if (key.Length > 0) query = query.Where(t => t.NormalizedName.Contains(key) || t.SearchKeys.Contains(key));

        query = filter switch
        {
            TagFilter.Used => query.Where(t => t.ProjectCount + t.ExperienceAreaCount + t.SkillGroupCount > 0),
            TagFilter.Unused => query.Where(t => t.ProjectCount + t.ExperienceAreaCount + t.SkillGroupCount == 0),
            _ => query
        };

        query = sort == TagSort.Usage
            ? query.OrderByDescending(t => t.ProjectCount + t.ExperienceAreaCount + t.SkillGroupCount)
                   .ThenBy(t => t.Name).ThenBy(t => t.Id)
            : query.OrderBy(t => t.Name).ThenBy(t => t.Id);

        return ToPagedAsync(query, page, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(int Total, int Used)> CountAsync(CancellationToken cancellationToken = default)
    {
        var total = await Set.CountAsync(cancellationToken);
        var used = await Set.CountAsync(t => t.ProjectCount + t.ExperienceAreaCount + t.SkillGroupCount > 0,
                                        cancellationToken);
        return (total, used);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetShortlistAsync(string name, int max,
                                                               CancellationToken cancellationToken = default)
    {
        var all = Set.AsNoTracking();
        if (await all.CountAsync(cancellationToken) <= SendAllToAiUpTo)
            return await all.OrderBy(t => t.Name).Select(t => t.Name).ToListAsync(cancellationToken);

        // Tags sharing a word (3+ letters) with the typed name: "restful services" → REST…, SERV…
        var words = TagNames.Clean(name).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Select(w => TagNames.Key(w))
                            .Where(w => w.Length >= 3)
                            .Select(w => w.Length > 6 ? w[..6] : w)
                            .Distinct()
                            .Take(6)
                            .ToList();

        var names = new List<string>();
        if (words.Count > 0)
        {
            names.AddRange(await all.Where(AnyToken(words))
                                    .OrderByDescending(t => t.ProjectCount + t.ExperienceAreaCount + t.SkillGroupCount)
                                    .Select(t => t.Name)
                                    .Take(max / 2)
                                    .ToListAsync(cancellationToken));
        }

        // …plus the most used tags, which most new names turn out to be
        names.AddRange(await all.OrderByDescending(t => t.ProjectCount + t.ExperienceAreaCount + t.SkillGroupCount)
                                .ThenBy(t => t.Name)
                                .Select(t => t.Name)
                                .Take(max)
                                .ToListAsync(cancellationToken));

        return names.Distinct().Take(max).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Tag>> GetUsedAsync(CancellationToken cancellationToken = default) =>
        await Set.Where(t => t.ProjectCount + t.ExperienceAreaCount + t.SkillGroupCount > 0)
                 .ToListAsync(cancellationToken);

    /// <summary>
    /// <c>NormalizedName = 'A' OR SearchKeys LIKE '%|A|%' OR …</c> for each key: the exact key always finds the tag,
    /// the search keys find it by an alias.
    /// </summary>
    private static Expression<Func<Tag, bool>> AnyKey(IReadOnlyList<string> keys)
    {
        var tag = Expression.Parameter(typeof(Tag), "t");
        var normalized = Expression.Property(tag, nameof(Tag.NormalizedName));
        var searchKeys = Expression.Property(tag, nameof(Tag.SearchKeys));
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        Expression? body = null;
        foreach (var key in keys)
        {
            var keyValue = Expression.Property(Expression.Constant(new TokenHolder(key)), nameof(TokenHolder.Value));
            var tokenValue = Expression.Property(Expression.Constant(new TokenHolder(Tag.SearchToken(key))),
                                                 nameof(TokenHolder.Value));
            Expression test = Expression.OrElse(Expression.Equal(normalized, keyValue),
                                                Expression.Call(searchKeys, contains, tokenValue));
            body = body is null ? test : Expression.OrElse(body, test);
        }

        return Expression.Lambda<Func<Tag, bool>>(body ?? Expression.Constant(false), tag);
    }

    /// <summary><c>SearchKeys LIKE '%a%' OR SearchKeys LIKE '%b%' …</c></summary>
    private static Expression<Func<Tag, bool>> AnyToken(IReadOnlyList<string> tokens)
    {
        var tag = Expression.Parameter(typeof(Tag), "t");
        var searchKeys = Expression.Property(tag, nameof(Tag.SearchKeys));
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        Expression? body = null;
        foreach (var token in tokens)
        {
            // A closure value, so EF sends each token as a parameter (never as SQL text)
            var value = Expression.Property(Expression.Constant(new TokenHolder(token)), nameof(TokenHolder.Value));
            Expression test = Expression.Call(searchKeys, contains, value);
            body = body is null ? test : Expression.OrElse(body, test);
        }

        return Expression.Lambda<Func<Tag, bool>>(body ?? Expression.Constant(false), tag);
    }

    private sealed record TokenHolder(string Value);
}
