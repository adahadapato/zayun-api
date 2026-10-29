using MediatR;
using Zayun.Application.Features.Tags.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Features.Tags.Queries;

/// <summary>Admin → Tags: every tag in the shared list, by name, with where it's used. Never cached.</summary>
public sealed record GetTagsQuery : IRequest<Result<IReadOnlyList<TagDto>>>;

/// <summary>Handles <see cref="GetTagsQuery"/>.</summary>
public sealed class GetTagsQueryHandler(ITagRepository tags, TagUsageCounter usage)
    : IRequestHandler<GetTagsQuery, Result<IReadOnlyList<TagDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TagDto>>> Handle(GetTagsQuery query, CancellationToken ct)
    {
        var all = await tags.GetAllAsync(ct);
        var usedBy = await usage.CountAsync(ct);
        return Result.Success<IReadOnlyList<TagDto>>(all.Select(t => TagDto.From(t, usedBy(t))).ToList());
    }
}

/// <summary>
/// The tag check while typing (project tags, experience skills, skill-group skills): is this name already a tag,
/// the same as one under another spelling, a new tag (and how is it normally spelled), or not a skill at all?
/// </summary>
/// <param name="Name">What was typed.</param>
/// <remarks>
/// The built-in rules answer first, for free (same key, synonyms, singular / plural). Only names they don't know go
/// to the AI. If the AI can't be reached the name is simply treated as new.
/// </remarks>
public sealed record CheckTagQuery(string Name) : IRequest<Result<TagCheckDto>>;

/// <summary>Handles <see cref="CheckTagQuery"/>.</summary>
public sealed class CheckTagQueryHandler(ITagRepository tags, ITagAdvisor advisor)
    : IRequestHandler<CheckTagQuery, Result<TagCheckDto>>
{
    /// <inheritdoc />
    public async Task<Result<TagCheckDto>> Handle(CheckTagQuery query, CancellationToken ct)
    {
        var typed = TagNames.Clean(query.Name);
        var all = await tags.GetAllAsync(ct);
        var vocabulary = new TagVocabulary(all);

        // 1. Rules: an existing tag (same key, alias, synonym, singular / plural)
        var existing = vocabulary.Find(typed);
        if (existing is not null)
        {
            return new TagCheckDto(TagCheckStatus.Existing, existing.Name, existing.Id,
                                   TagNames.Key(typed) == existing.NormalizedName && typed == existing.Name
                                       ? "Already a tag."
                                       : $"Same as \"{existing.Name}\".",
                                   CheckedByAi: false);
        }

        // 2. Rules: a well-known name or synonym that isn't in the list yet
        var builtIn = TagNames.BuiltIn(typed);
        if (builtIn is not null)
            return New(builtIn, builtIn == typed ? "New tag." : $"New tag, written \"{builtIn}\".", ai: false);

        // 3. The AI, when it's on
        if (!advisor.IsAvailable) return New(typed, "New tag.", ai: false);

        var advice = await advisor.AdviseAsync(typed, all.Select(t => t.Name).ToList(), ct);
        if (advice is null) return New(typed, "New tag (the AI check couldn't be reached).", ai: false);

        switch (advice.Verdict)
        {
            case TagAdviceVerdict.SameAsExisting:
                var match = vocabulary.Find(advice.Name);
                if (match is not null)
                {
                    return new TagCheckDto(TagCheckStatus.Existing, match.Name, match.Id,
                                           Explain($"Means the same as \"{match.Name}\".", advice.Reason),
                                           CheckedByAi: true);
                }
                return New(typed, "New tag.", ai: true);

            case TagAdviceVerdict.NewTag:
                var spelled = TagNames.Clean(advice.Name);
                if (spelled.Length == 0 || spelled.Length > TagNames.MaxLength) spelled = typed;

                var spelledMatch = vocabulary.Find(spelled);
                if (spelledMatch is not null)
                {
                    return new TagCheckDto(TagCheckStatus.Existing, spelledMatch.Name, spelledMatch.Id,
                                           $"Same as \"{spelledMatch.Name}\".", CheckedByAi: true);
                }
                return New(spelled, spelled == typed ? "New tag." : $"New tag, written \"{spelled}\".", ai: true);

            default:
                return new TagCheckDto(TagCheckStatus.Doubtful, typed, null,
                                       advice.Reason is { Length: > 0 } reason
                                           ? reason
                                           : "This doesn't look like a skill or technology.",
                                       CheckedByAi: true);
        }
    }

    private static TagCheckDto New(string name, string message, bool ai) =>
        new(TagCheckStatus.New, name, null, message, ai);

    private static string Explain(string message, string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? message : $"{message} {reason}";
}
