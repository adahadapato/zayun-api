using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Tags;
using Zayun.Application.Common.Content;
using Zayun.Application.Features.Experience.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Experience.Commands;

/// <summary>Admin → New experience area. It goes to the end of the list.</summary>
/// <param name="Title">The area's name.</param>
/// <param name="Description">What it involved.</param>
/// <param name="Skills">Skills used.</param>
/// <param name="IsPublished">Show it straight away, or keep it as a draft.</param>
[InvalidatesCache(CacheTags.Experience)]
[RefreshesTagUsage]
public sealed record CreateExperienceAreaCommand(string Title, string Description, IReadOnlyList<string>? Skills,
                                                 bool IsPublished) : IRequest<Result<ExperienceAreaDto>>;

/// <summary>Handles <see cref="CreateExperienceAreaCommand"/>.</summary>
public sealed class CreateExperienceAreaCommandHandler(IExperienceAreaRepository areas, IUnitOfWork unitOfWork,
                                                       TagResolver tags)
    : IRequestHandler<CreateExperienceAreaCommand, Result<ExperienceAreaDto>>
{
    /// <inheritdoc />
    public async Task<Result<ExperienceAreaDto>> Handle(CreateExperienceAreaCommand c, CancellationToken ct)
    {
        var area = ExperienceArea.Create(c.Title, c.Description, await tags.ResolveAsync(c.Skills, ct),
                                         await areas.GetNextDisplayOrderAsync(ct), c.IsPublished);
        areas.Add(area);
        await unitOfWork.SaveChangesAsync(ct);
        return ExperienceAreaDto.From(area);
    }
}

/// <summary>Admin → Edit experience area: its text and skills.</summary>
/// <param name="Id">The area.</param>
/// <param name="Title">The area's name.</param>
/// <param name="Description">What it involved.</param>
/// <param name="Skills">Skills used (replaces the old list).</param>
[InvalidatesCache(CacheTags.Experience)]
[RefreshesTagUsage]
public sealed record UpdateExperienceAreaCommand(Guid Id, string Title, string Description,
                                                 IReadOnlyList<string>? Skills) : IRequest<Result<ExperienceAreaDto>>;

/// <summary>Handles <see cref="UpdateExperienceAreaCommand"/>.</summary>
public sealed class UpdateExperienceAreaCommandHandler(IExperienceAreaRepository areas, IUnitOfWork unitOfWork,
                                                       TagResolver tags)
    : IRequestHandler<UpdateExperienceAreaCommand, Result<ExperienceAreaDto>>
{
    /// <inheritdoc />
    public async Task<Result<ExperienceAreaDto>> Handle(UpdateExperienceAreaCommand c, CancellationToken ct)
    {
        var area = await areas.GetByIdAsync(c.Id, ct);
        if (area is null) return ExperienceErrors.NotFound;

        area.Update(c.Title, c.Description, await tags.ResolveAsync(c.Skills, ct));
        await unitOfWork.SaveChangesAsync(ct);
        return ExperienceAreaDto.From(area);
    }
}

/// <summary>Admin → Experience: show or hide an area.</summary>
/// <param name="Id">The area.</param>
/// <param name="IsPublished">True to show it.</param>
[InvalidatesCache(CacheTags.Experience)]
public sealed record SetExperienceAreaPublishedCommand(Guid Id, bool IsPublished) : IRequest<Result>;

/// <summary>Handles <see cref="SetExperienceAreaPublishedCommand"/>.</summary>
public sealed class SetExperienceAreaPublishedCommandHandler(IExperienceAreaRepository areas, IUnitOfWork unitOfWork)
    : IRequestHandler<SetExperienceAreaPublishedCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(SetExperienceAreaPublishedCommand c, CancellationToken ct) =>
        ContentActions.SetPublishedAsync(areas, unitOfWork, c.Id, c.IsPublished, ExperienceErrors.NotFound, ct);
}

/// <summary>Admin → Experience: delete an area (soft delete).</summary>
/// <param name="Id">The area.</param>
[InvalidatesCache(CacheTags.Experience)]
[RefreshesTagUsage]
public sealed record DeleteExperienceAreaCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteExperienceAreaCommand"/>.</summary>
public sealed class DeleteExperienceAreaCommandHandler(IExperienceAreaRepository areas, IUnitOfWork unitOfWork,
                                                       TimeProvider clock)
    : IRequestHandler<DeleteExperienceAreaCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(DeleteExperienceAreaCommand c, CancellationToken ct) =>
        ContentActions.DeleteAsync(areas, unitOfWork, clock, c.Id, ExperienceErrors.NotFound, ct);
}

/// <summary>Admin → Experience: drag-to-reorder. Send every area's Id in the new order.</summary>
/// <param name="Ids">Every area's Id, each once.</param>
[InvalidatesCache(CacheTags.Experience)]
public sealed record ReorderExperienceAreasCommand(IReadOnlyList<Guid> Ids) : IRequest<Result>;

/// <summary>Handles <see cref="ReorderExperienceAreasCommand"/>.</summary>
public sealed class ReorderExperienceAreasCommandHandler(IExperienceAreaRepository areas, IUnitOfWork unitOfWork)
    : IRequestHandler<ReorderExperienceAreasCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ReorderExperienceAreasCommand c, CancellationToken ct) =>
        ContentActions.ReorderAsync(areas, unitOfWork, c.Ids, ct);
}
