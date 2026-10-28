using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Content;
using Zayun.Application.Features.Skills.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Skills.Commands;

/// <summary>Admin → Skills → Add core expertise. It goes to the end of the list.</summary>
/// <param name="Name">The expertise, e.g. "Explainable AI".</param>
/// <param name="IsPublished">Show it straight away, or keep it as a draft.</param>
[InvalidatesCache(CacheTags.Skills)]
public sealed record CreateCoreExpertiseCommand(string Name, bool IsPublished) : IRequest<Result<CoreExpertiseDto>>;

/// <summary>Handles <see cref="CreateCoreExpertiseCommand"/>.</summary>
public sealed class CreateCoreExpertiseCommandHandler(ICoreExpertiseRepository expertise, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCoreExpertiseCommand, Result<CoreExpertiseDto>>
{
    /// <inheritdoc />
    public async Task<Result<CoreExpertiseDto>> Handle(CreateCoreExpertiseCommand c, CancellationToken ct)
    {
        if (await expertise.NameExistsAsync(c.Name, exceptId: null, ct)) return SkillErrors.ExpertiseExists;

        var item = CoreExpertise.Create(c.Name, await expertise.GetNextDisplayOrderAsync(ct), c.IsPublished);
        expertise.Add(item);
        await unitOfWork.SaveChangesAsync(ct);
        return CoreExpertiseDto.From(item);
    }
}

/// <summary>Admin → Skills: rename a core expertise item.</summary>
/// <param name="Id">The item.</param>
/// <param name="Name">The new name.</param>
[InvalidatesCache(CacheTags.Skills)]
public sealed record RenameCoreExpertiseCommand(Guid Id, string Name) : IRequest<Result<CoreExpertiseDto>>;

/// <summary>Handles <see cref="RenameCoreExpertiseCommand"/>.</summary>
public sealed class RenameCoreExpertiseCommandHandler(ICoreExpertiseRepository expertise, IUnitOfWork unitOfWork)
    : IRequestHandler<RenameCoreExpertiseCommand, Result<CoreExpertiseDto>>
{
    /// <inheritdoc />
    public async Task<Result<CoreExpertiseDto>> Handle(RenameCoreExpertiseCommand c, CancellationToken ct)
    {
        var item = await expertise.GetByIdAsync(c.Id, ct);
        if (item is null) return SkillErrors.ExpertiseNotFound;
        if (await expertise.NameExistsAsync(c.Name, c.Id, ct)) return SkillErrors.ExpertiseExists;

        item.Rename(c.Name);
        await unitOfWork.SaveChangesAsync(ct);
        return CoreExpertiseDto.From(item);
    }
}

/// <summary>Admin → Skills: show or hide a core expertise item.</summary>
/// <param name="Id">The item.</param>
/// <param name="IsPublished">True to show it.</param>
[InvalidatesCache(CacheTags.Skills)]
public sealed record SetCoreExpertisePublishedCommand(Guid Id, bool IsPublished) : IRequest<Result>;

/// <summary>Handles <see cref="SetCoreExpertisePublishedCommand"/>.</summary>
public sealed class SetCoreExpertisePublishedCommandHandler(ICoreExpertiseRepository expertise, IUnitOfWork unitOfWork)
    : IRequestHandler<SetCoreExpertisePublishedCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(SetCoreExpertisePublishedCommand c, CancellationToken ct) =>
        ContentActions.SetPublishedAsync(expertise, unitOfWork, c.Id, c.IsPublished, SkillErrors.ExpertiseNotFound, ct);
}

/// <summary>Admin → Skills: delete a core expertise item (soft delete).</summary>
/// <param name="Id">The item.</param>
[InvalidatesCache(CacheTags.Skills)]
public sealed record DeleteCoreExpertiseCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteCoreExpertiseCommand"/>.</summary>
public sealed class DeleteCoreExpertiseCommandHandler(ICoreExpertiseRepository expertise, IUnitOfWork unitOfWork,
                                                      TimeProvider clock)
    : IRequestHandler<DeleteCoreExpertiseCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(DeleteCoreExpertiseCommand c, CancellationToken ct) =>
        ContentActions.DeleteAsync(expertise, unitOfWork, clock, c.Id, SkillErrors.ExpertiseNotFound, ct);
}

/// <summary>Admin → Skills: drag-to-reorder the core expertise list. Send every item's Id in the new order.</summary>
/// <param name="Ids">Every item's Id, each once.</param>
[InvalidatesCache(CacheTags.Skills)]
public sealed record ReorderCoreExpertiseCommand(IReadOnlyList<Guid> Ids) : IRequest<Result>;

/// <summary>Handles <see cref="ReorderCoreExpertiseCommand"/>.</summary>
public sealed class ReorderCoreExpertiseCommandHandler(ICoreExpertiseRepository expertise, IUnitOfWork unitOfWork)
    : IRequestHandler<ReorderCoreExpertiseCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ReorderCoreExpertiseCommand c, CancellationToken ct) =>
        ContentActions.ReorderAsync(expertise, unitOfWork, c.Ids, ct);
}
