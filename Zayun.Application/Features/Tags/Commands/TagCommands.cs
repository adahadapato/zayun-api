using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Tags;
using Zayun.Application.Features.Tags.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Features.Tags.Commands;

/// <summary>Admin → Tags → New tag (tags are also added automatically when an item using a new name is saved).</summary>
/// <param name="Name">The name as it should be shown.</param>
public sealed record CreateTagCommand(string Name) : IRequest<Result<TagDto>>;

/// <summary>Handles <see cref="CreateTagCommand"/>.</summary>
public sealed class CreateTagCommandHandler(ITagRepository tags, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTagCommand, Result<TagDto>>
{
    /// <inheritdoc />
    public async Task<Result<TagDto>> Handle(CreateTagCommand c, CancellationToken ct)
    {
        var vocabulary = new TagVocabulary(await tags.FindByKeysAsync(TagVocabulary.CandidateKeys([c.Name]), ct));
        var existing = vocabulary.Find(c.Name);
        if (existing is not null) return TagErrors.AlreadyExists(existing.Name);

        var tag = vocabulary.Resolve(c.Name)!;
        tags.Add(tag);
        await unitOfWork.SaveChangesAsync(ct);
        return TagDto.From(tag);
    }
}

/// <summary>
/// Admin → Tags → Rename: changes a tag's name everywhere it's used (projects, experience, skills). The old name is
/// kept as an alias, so typing it later still finds the tag.
/// </summary>
/// <param name="Id">The tag.</param>
/// <param name="Name">The new name.</param>
[InvalidatesCache(CacheTags.Projects, CacheTags.Experience, CacheTags.Skills)]
[RefreshesTagUsage]
public sealed record RenameTagCommand(Guid Id, string Name) : IRequest<Result<TagDto>>;

/// <summary>Handles <see cref="RenameTagCommand"/>.</summary>
public sealed class RenameTagCommandHandler(ITagRepository tags, TaggedContent content, IUnitOfWork unitOfWork)
    : IRequestHandler<RenameTagCommand, Result<TagDto>>
{
    /// <inheritdoc />
    public async Task<Result<TagDto>> Handle(RenameTagCommand c, CancellationToken ct)
    {
        var tag = await tags.GetByIdAsync(c.Id, ct);
        if (tag is null) return TagErrors.NotFound;

        var newKey = TagNames.Key(c.Name);
        var clash = (await tags.FindByKeysAsync([newKey], ct)).FirstOrDefault(t => t.Id != tag.Id);
        if (clash is not null) return TagErrors.AlreadyExists(clash.Name);

        var oldName = tag.Rename(c.Name);
        if (oldName != tag.Name) await content.ReplaceAsync(oldName, tag.Name, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return TagDto.From(tag);
    }
}

/// <summary>
/// Admin → Tags → Merge: two tags that mean the same thing become one. Everything using the first now uses the
/// second; the first's names become aliases of the second, and the first is deleted.
/// </summary>
/// <param name="Id">The tag to merge (it goes away).</param>
/// <param name="IntoTagId">The tag to keep.</param>
[InvalidatesCache(CacheTags.Projects, CacheTags.Experience, CacheTags.Skills)]
[RefreshesTagUsage]
public sealed record MergeTagCommand(Guid Id, Guid IntoTagId) : IRequest<Result<TagDto>>;

/// <summary>Handles <see cref="MergeTagCommand"/>.</summary>
public sealed class MergeTagCommandHandler(ITagRepository tags, TaggedContent content, IUnitOfWork unitOfWork)
    : IRequestHandler<MergeTagCommand, Result<TagDto>>
{
    /// <inheritdoc />
    public async Task<Result<TagDto>> Handle(MergeTagCommand c, CancellationToken ct)
    {
        if (c.Id == c.IntoTagId) return TagErrors.MergeIntoSelf;

        var source = await tags.GetByIdAsync(c.Id, ct);
        var target = await tags.GetByIdAsync(c.IntoTagId, ct);
        if (source is null || target is null) return TagErrors.NotFound;

        await content.ReplaceAsync(source.Name, target.Name, ct);
        target.Absorb(source);
        tags.Remove(source);
        await unitOfWork.SaveChangesAsync(ct);
        return TagDto.From(target);
    }
}

/// <summary>Admin → Tags → Delete: only a tag nothing uses (merge it otherwise).</summary>
/// <param name="Id">The tag.</param>
public sealed record DeleteTagCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteTagCommand"/>.</summary>
public sealed class DeleteTagCommandHandler(ITagRepository tags, TagUsageRefresher usage, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTagCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeleteTagCommand c, CancellationToken ct)
    {
        // Counts first, so a tag that was just put on an item can't slip through
        await usage.RefreshAsync(ct);

        var tag = await tags.GetByIdAsync(c.Id, ct);
        if (tag is null) return TagErrors.NotFound;
        if (tag.UsageCount > 0) return TagErrors.InUse(tag.UsageCount);

        tags.Remove(tag);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

/// <summary>Everything that holds tag names: project tags, experience skills and skill-group skills.</summary>
public sealed class TaggedContent(IProjectRepository projects, IExperienceAreaRepository experience,
                                  ISkillGroupRepository skillGroups)
{
    /// <summary>Swaps one name for another everywhere (drafts included). Saved by the caller.</summary>
    /// <param name="oldName">The name used now.</param>
    /// <param name="newName">The name to use instead.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    public async Task ReplaceAsync(string oldName, string newName, CancellationToken cancellationToken)
    {
        foreach (var project in await projects.GetAllForUpdateAsync(cancellationToken))
            project.ReplaceTag(oldName, newName);
        foreach (var area in await experience.GetAllForUpdateAsync(cancellationToken))
            area.ReplaceSkill(oldName, newName);
        foreach (var group in await skillGroups.GetAllForUpdateAsync(cancellationToken))
            group.ReplaceSkill(oldName, newName);
    }
}
