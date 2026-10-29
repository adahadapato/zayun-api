using MediatR;
using Zayun.Application.Features.Tags.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Features.Tags.Queries;

/// <summary>
/// Admin → Tags (and the tag boxes' suggestions): one page of the shared tag list, searched, filtered and sorted in
/// the database. Never cached.
/// </summary>
/// <param name="Search">Text in the name or an old name (any case; spaces and dots ignored).</param>
/// <param name="Filter">All, Used or Unused.</param>
/// <param name="Sort">Name (A–Z) or Usage (most used first).</param>
/// <param name="Page">Page number, from 1.</param>
/// <param name="PageSize">Tags per page (1–100).</param>
public sealed record GetTagsQuery(
    string? Search = null,
    TagFilter Filter = TagFilter.All,
    TagSort Sort = TagSort.Name,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<TagListDto>>;

/// <summary>Handles <see cref="GetTagsQuery"/>.</summary>
public sealed class GetTagsQueryHandler(ITagRepository tags) : IRequestHandler<GetTagsQuery, Result<TagListDto>>
{
    /// <inheritdoc />
    public async Task<Result<TagListDto>> Handle(GetTagsQuery query, CancellationToken ct)
    {
        var page = await tags.SearchAsync(query.Search, query.Filter, query.Sort,
                                          new PageRequest { Page = query.Page, PageSize = query.PageSize }, ct);
        var (total, used) = await tags.CountAsync(ct);

        return new TagListDto(
            new PagedResult<TagDto>(page.Items.Select(TagDto.From).ToList(), page.TotalCount, page.Page, page.PageSize),
            total, used, total - used);
    }
}

/// <summary>
/// The tag check while typing (project tags, experience skills, skill-group skills): is this name already a tag,
/// the same as one under another spelling, a new tag (and how is it normally spelled), or not a skill at all?
/// </summary>
/// <param name="Name">What was typed.</param>
/// <remarks>
/// The built-in rules answer first, for free (same key, synonyms, singular / plural). Only names they don't know go
/// to the AI, with a shortlist of tags (all of them while the list is small). If the AI can't be reached the name is
/// simply treated as new.
/// </remarks>
public sealed record CheckTagQuery(string Name) : IRequest<Result<TagCheckDto>>;

/// <summary>Handles <see cref="CheckTagQuery"/>.</summary>
public sealed class CheckTagQueryHandler(ITagRepository tags, ITagAdvisor advisor)
    : IRequestHandler<CheckTagQuery, Result<TagCheckDto>>
{
    /// <summary>Most tag names sent to the AI.</summary>
    private const int AiShortlist = 300;

    /// <inheritdoc />
    public async Task<Result<TagCheckDto>> Handle(CheckTagQuery query, CancellationToken ct)
    {
        var typed = TagNames.Clean(query.Name);

        // 1. Rules: an existing tag (same key, alias, synonym, singular / plural)
        var existing = await FindAsync(typed, ct);
        if (existing is not null)
        {
            return new TagCheckDto(TagCheckStatus.Existing, existing.Name, existing.Id,
                                   typed == existing.Name ? "Already a tag." : $"Same as \"{existing.Name}\".",
                                   CheckedByAi: false);
        }

        // 2. Rules: a well-known name or synonym that isn't in the list yet
        var builtIn = TagNames.BuiltIn(typed);
        if (builtIn is not null)
            return New(builtIn, builtIn == typed ? "New tag." : $"New tag, written \"{builtIn}\".", ai: false);

        // 3. The AI, when it's on
        if (!advisor.IsAvailable) return New(typed, "New tag.", ai: false);

        var shortlist = await tags.GetShortlistAsync(typed, AiShortlist, ct);
        var advice = await advisor.AdviseAsync(typed, shortlist, ct);
        if (advice is null) return New(typed, "New tag (the AI check couldn't be reached).", ai: false);

        switch (advice.Verdict)
        {
            case TagAdviceVerdict.SameAsExisting:
                var match = await FindAsync(advice.Name, ct);
                return match is not null
                    ? new TagCheckDto(TagCheckStatus.Existing, match.Name, match.Id,
                                      Explain($"Means the same as \"{match.Name}\".", advice.Reason), CheckedByAi: true)
                    : New(typed, "New tag.", ai: true);

            case TagAdviceVerdict.NewTag:
                var spelled = TagNames.Clean(advice.Name);
                if (spelled.Length == 0 || spelled.Length > TagNames.MaxLength) spelled = typed;

                var spelledMatch = spelled == typed ? null : await FindAsync(spelled, ct);
                return spelledMatch is not null
                    ? new TagCheckDto(TagCheckStatus.Existing, spelledMatch.Name, spelledMatch.Id,
                                      $"Same as \"{spelledMatch.Name}\".", CheckedByAi: true)
                    : New(spelled, spelled == typed ? "New tag." : $"New tag, written \"{spelled}\".", ai: true);

            default:
                return new TagCheckDto(TagCheckStatus.Doubtful, typed, null,
                                       advice.Reason is { Length: > 0 } reason
                                           ? reason
                                           : "This doesn't look like a skill or technology.",
                                       CheckedByAi: true);
        }
    }

    /// <summary>An existing tag for a name, loading only the tags that could match.</summary>
    private async Task<Tag?> FindAsync(string name, CancellationToken ct) =>
        new TagVocabulary(await tags.FindByKeysAsync(TagVocabulary.CandidateKeys([name]), ct)).Find(name);

    private static TagCheckDto New(string name, string message, bool ai) =>
        new(TagCheckStatus.New, name, null, message, ai);

    private static string Explain(string message, string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? message : $"{message} {reason}";
}
