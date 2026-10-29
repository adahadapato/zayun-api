using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Tags;
using Zayun.Application.Common.Content;
using Zayun.Application.Features.Skills.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Skills.Commands;

/// <summary>Admin → New skill group. It goes to the end of the list.</summary>
/// <param name="Title">The group's name.</param>
/// <param name="Description">What the group covers.</param>
/// <param name="Skills">The skills.</param>
/// <param name="IsPublished">Show it straight away, or keep it as a draft.</param>
[InvalidatesCache(CacheTags.Skills)]
public sealed record CreateSkillGroupCommand(string Title, string Description, IReadOnlyList<string>? Skills,
                                                 bool IsPublished) : IRequest<Result<SkillGroupDto>>;

/// <summary>Handles <see cref="CreateSkillGroupCommand"/>.</summary>
public sealed class CreateSkillGroupCommandHandler(ISkillGroupRepository groups, IUnitOfWork unitOfWork,
                                                   TagResolver tags)
    : IRequestHandler<CreateSkillGroupCommand, Result<SkillGroupDto>>
{
    /// <inheritdoc />
    public async Task<Result<SkillGroupDto>> Handle(CreateSkillGroupCommand c, CancellationToken ct)
    {
        var group = SkillGroup.Create(c.Title, c.Description, await tags.ResolveAsync(c.Skills, ct),
                                         await groups.GetNextDisplayOrderAsync(ct), c.IsPublished);
        groups.Add(group);
        await unitOfWork.SaveChangesAsync(ct);
        return SkillGroupDto.From(group);
    }
}

/// <summary>Admin → Edit skill group: its text and skills.</summary>
/// <param name="Id">The group.</param>
/// <param name="Title">The group's name.</param>
/// <param name="Description">What the group covers.</param>
/// <param name="Skills">The skills (replaces the old list).</param>
[InvalidatesCache(CacheTags.Skills)]
public sealed record UpdateSkillGroupCommand(Guid Id, string Title, string Description,
                                                 IReadOnlyList<string>? Skills) : IRequest<Result<SkillGroupDto>>;

/// <summary>Handles <see cref="UpdateSkillGroupCommand"/>.</summary>
public sealed class UpdateSkillGroupCommandHandler(ISkillGroupRepository groups, IUnitOfWork unitOfWork,
                                                   TagResolver tags)
    : IRequestHandler<UpdateSkillGroupCommand, Result<SkillGroupDto>>
{
    /// <inheritdoc />
    public async Task<Result<SkillGroupDto>> Handle(UpdateSkillGroupCommand c, CancellationToken ct)
    {
        var group = await groups.GetByIdAsync(c.Id, ct);
        if (group is null) return SkillErrors.GroupNotFound;

        group.Update(c.Title, c.Description, await tags.ResolveAsync(c.Skills, ct));
        await unitOfWork.SaveChangesAsync(ct);
        return SkillGroupDto.From(group);
    }
}

/// <summary>Admin → Skills: show or hide a group.</summary>
/// <param name="Id">The group.</param>
/// <param name="IsPublished">True to show it.</param>
[InvalidatesCache(CacheTags.Skills)]
public sealed record SetSkillGroupPublishedCommand(Guid Id, bool IsPublished) : IRequest<Result>;

/// <summary>Handles <see cref="SetSkillGroupPublishedCommand"/>.</summary>
public sealed class SetSkillGroupPublishedCommandHandler(ISkillGroupRepository groups, IUnitOfWork unitOfWork)
    : IRequestHandler<SetSkillGroupPublishedCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(SetSkillGroupPublishedCommand c, CancellationToken ct) =>
        ContentActions.SetPublishedAsync(groups, unitOfWork, c.Id, c.IsPublished, SkillErrors.GroupNotFound, ct);
}

/// <summary>Admin → Skills: delete a group (soft delete).</summary>
/// <param name="Id">The group.</param>
[InvalidatesCache(CacheTags.Skills)]
public sealed record DeleteSkillGroupCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteSkillGroupCommand"/>.</summary>
public sealed class DeleteSkillGroupCommandHandler(ISkillGroupRepository groups, IUnitOfWork unitOfWork,
                                                       TimeProvider clock)
    : IRequestHandler<DeleteSkillGroupCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(DeleteSkillGroupCommand c, CancellationToken ct) =>
        ContentActions.DeleteAsync(groups, unitOfWork, clock, c.Id, SkillErrors.GroupNotFound, ct);
}

/// <summary>Admin → Skills: drag-to-reorder the groups. Send every group's Id in the new order.</summary>
/// <param name="Ids">Every group's Id, each once.</param>
[InvalidatesCache(CacheTags.Skills)]
public sealed record ReorderSkillGroupsCommand(IReadOnlyList<Guid> Ids) : IRequest<Result>;

/// <summary>Handles <see cref="ReorderSkillGroupsCommand"/>.</summary>
public sealed class ReorderSkillGroupsCommandHandler(ISkillGroupRepository groups, IUnitOfWork unitOfWork)
    : IRequestHandler<ReorderSkillGroupsCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ReorderSkillGroupsCommand c, CancellationToken ct) =>
        ContentActions.ReorderAsync(groups, unitOfWork, c.Ids, ct);
}
