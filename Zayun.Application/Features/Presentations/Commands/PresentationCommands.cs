using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Content;
using Zayun.Application.Features.Presentations.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Presentations.Commands;

/// <summary>Admin → New presentation (add its picture afterwards). It goes to the end of the list.</summary>
/// <param name="Title">What was presented.</param>
/// <param name="EventName">Where and when.</param>
/// <param name="Year">The year.</param>
/// <param name="Type">Poster, oral, conference…</param>
/// <param name="IsPublished">Show it straight away, or keep it as a draft.</param>
[InvalidatesCache(CacheTags.Presentations)]
public sealed record CreatePresentationCommand(
    string Title,
    string EventName,
    int Year,
    PresentationType Type,
    bool IsPublished) : IRequest<Result<PresentationDto>>;

/// <summary>Handles <see cref="CreatePresentationCommand"/>.</summary>
public sealed class CreatePresentationCommandHandler(IPresentationRepository presentations, IUnitOfWork unitOfWork,
                                                     IFileStorage storage)
    : IRequestHandler<CreatePresentationCommand, Result<PresentationDto>>
{
    /// <inheritdoc />
    public async Task<Result<PresentationDto>> Handle(CreatePresentationCommand c, CancellationToken ct)
    {
        var presentation = Presentation.Create(
            new PresentationDetails(c.Title, c.EventName, c.Year, c.Type, ImageAlt: null),
            await presentations.GetNextDisplayOrderAsync(ct),
            c.IsPublished);

        presentations.Add(presentation);
        await unitOfWork.SaveChangesAsync(ct);
        return PresentationDto.From(presentation, storage);
    }
}

/// <summary>Admin → Edit presentation: everything except its place, published state and picture file.</summary>
/// <param name="Id">The presentation.</param>
/// <param name="Title">What was presented.</param>
/// <param name="EventName">Where and when.</param>
/// <param name="Year">The year.</param>
/// <param name="Type">Poster, oral, conference…</param>
/// <param name="ImageAlt">New alt text for the picture; null leaves it as it is.</param>
[InvalidatesCache(CacheTags.Presentations)]
public sealed record UpdatePresentationCommand(
    Guid Id,
    string Title,
    string EventName,
    int Year,
    PresentationType Type,
    string? ImageAlt) : IRequest<Result<PresentationDto>>;

/// <summary>Handles <see cref="UpdatePresentationCommand"/>.</summary>
public sealed class UpdatePresentationCommandHandler(IPresentationRepository presentations, IUnitOfWork unitOfWork,
                                                     IFileStorage storage)
    : IRequestHandler<UpdatePresentationCommand, Result<PresentationDto>>
{
    /// <inheritdoc />
    public async Task<Result<PresentationDto>> Handle(UpdatePresentationCommand c, CancellationToken ct)
    {
        var presentation = await presentations.GetByIdAsync(c.Id, ct);
        if (presentation is null) return PresentationErrors.NotFound;

        presentation.Update(new PresentationDetails(c.Title, c.EventName, c.Year, c.Type,
                                                    string.IsNullOrWhiteSpace(c.ImageAlt) ? null : c.ImageAlt));
        await unitOfWork.SaveChangesAsync(ct);
        return PresentationDto.From(presentation, storage);
    }
}

/// <summary>Admin → Presentations: show or hide a presentation.</summary>
/// <param name="Id">The presentation.</param>
/// <param name="IsPublished">True to show it.</param>
[InvalidatesCache(CacheTags.Presentations)]
public sealed record SetPresentationPublishedCommand(Guid Id, bool IsPublished) : IRequest<Result>;

/// <summary>Handles <see cref="SetPresentationPublishedCommand"/>.</summary>
public sealed class SetPresentationPublishedCommandHandler(IPresentationRepository presentations,
                                                           IUnitOfWork unitOfWork)
    : IRequestHandler<SetPresentationPublishedCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(SetPresentationPublishedCommand c, CancellationToken ct) =>
        ContentActions.SetPublishedAsync(presentations, unitOfWork, c.Id, c.IsPublished,
                                         PresentationErrors.NotFound, ct);
}

/// <summary>Admin → Presentations: delete a presentation (soft delete; its picture files are kept).</summary>
/// <param name="Id">The presentation.</param>
[InvalidatesCache(CacheTags.Presentations)]
public sealed record DeletePresentationCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeletePresentationCommand"/>.</summary>
public sealed class DeletePresentationCommandHandler(IPresentationRepository presentations, IUnitOfWork unitOfWork,
                                                     TimeProvider clock)
    : IRequestHandler<DeletePresentationCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(DeletePresentationCommand c, CancellationToken ct) =>
        ContentActions.DeleteAsync(presentations, unitOfWork, clock, c.Id, PresentationErrors.NotFound, ct);
}

/// <summary>Admin → Presentations: drag-to-reorder. Send every presentation's Id in the new order.</summary>
/// <param name="Ids">Every presentation's Id, each once.</param>
[InvalidatesCache(CacheTags.Presentations)]
public sealed record ReorderPresentationsCommand(IReadOnlyList<Guid> Ids) : IRequest<Result>;

/// <summary>Handles <see cref="ReorderPresentationsCommand"/>.</summary>
public sealed class ReorderPresentationsCommandHandler(IPresentationRepository presentations, IUnitOfWork unitOfWork)
    : IRequestHandler<ReorderPresentationsCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ReorderPresentationsCommand c, CancellationToken ct) =>
        ContentActions.ReorderAsync(presentations, unitOfWork, c.Ids, ct);
}
