using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Content;
using Zayun.Application.Features.Publications.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Publications.Commands;

/// <summary>Admin → New publication. It goes to the end of the list.</summary>
/// <param name="Title">The paper's title.</param>
/// <param name="Authors">Everyone who wrote it.</param>
/// <param name="Venue">Where it appeared.</param>
/// <param name="Year">The year.</param>
/// <param name="Type">Conference paper, journal article…</param>
/// <param name="Award">Optional award.</param>
/// <param name="Links">Where to read it.</param>
/// <param name="IsPublished">Show it straight away, or keep it as a draft.</param>
[InvalidatesCache(CacheTags.Publications)]
public sealed record CreatePublicationCommand(
    string Title,
    string Authors,
    string Venue,
    int Year,
    PublicationType Type,
    string? Award,
    IReadOnlyList<PublicationLinkDto>? Links,
    bool IsPublished) : IRequest<Result<PublicationDto>>;

/// <summary>Handles <see cref="CreatePublicationCommand"/>.</summary>
public sealed class CreatePublicationCommandHandler(IPublicationRepository publications, IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePublicationCommand, Result<PublicationDto>>
{
    /// <inheritdoc />
    public async Task<Result<PublicationDto>> Handle(CreatePublicationCommand c, CancellationToken ct)
    {
        var publication = Publication.Create(
            PublicationMapping.Details(c.Title, c.Authors, c.Venue, c.Year, c.Type, c.Award, c.Links),
            await publications.GetNextDisplayOrderAsync(ct),
            c.IsPublished);

        publications.Add(publication);
        await unitOfWork.SaveChangesAsync(ct);
        return PublicationDto.From(publication);
    }
}

/// <summary>Admin → Edit publication: everything except its place and published state.</summary>
/// <param name="Id">The publication.</param>
/// <param name="Title">The paper's title.</param>
/// <param name="Authors">Everyone who wrote it.</param>
/// <param name="Venue">Where it appeared.</param>
/// <param name="Year">The year.</param>
/// <param name="Type">Conference paper, journal article…</param>
/// <param name="Award">Optional award.</param>
/// <param name="Links">Where to read it (replaces the old list).</param>
[InvalidatesCache(CacheTags.Publications)]
public sealed record UpdatePublicationCommand(
    Guid Id,
    string Title,
    string Authors,
    string Venue,
    int Year,
    PublicationType Type,
    string? Award,
    IReadOnlyList<PublicationLinkDto>? Links) : IRequest<Result<PublicationDto>>;

/// <summary>Handles <see cref="UpdatePublicationCommand"/>.</summary>
public sealed class UpdatePublicationCommandHandler(IPublicationRepository publications, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePublicationCommand, Result<PublicationDto>>
{
    /// <inheritdoc />
    public async Task<Result<PublicationDto>> Handle(UpdatePublicationCommand c, CancellationToken ct)
    {
        var publication = await publications.GetByIdAsync(c.Id, ct);
        if (publication is null) return PublicationErrors.NotFound;

        publication.Update(PublicationMapping.Details(c.Title, c.Authors, c.Venue, c.Year, c.Type, c.Award, c.Links));
        await unitOfWork.SaveChangesAsync(ct);
        return PublicationDto.From(publication);
    }
}

/// <summary>Admin → Publications: show or hide a publication.</summary>
/// <param name="Id">The publication.</param>
/// <param name="IsPublished">True to show it.</param>
[InvalidatesCache(CacheTags.Publications)]
public sealed record SetPublicationPublishedCommand(Guid Id, bool IsPublished) : IRequest<Result>;

/// <summary>Handles <see cref="SetPublicationPublishedCommand"/>.</summary>
public sealed class SetPublicationPublishedCommandHandler(IPublicationRepository publications, IUnitOfWork unitOfWork)
    : IRequestHandler<SetPublicationPublishedCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(SetPublicationPublishedCommand c, CancellationToken ct) =>
        ContentActions.SetPublishedAsync(publications, unitOfWork, c.Id, c.IsPublished, PublicationErrors.NotFound, ct);
}

/// <summary>Admin → Publications: delete a publication (soft delete).</summary>
/// <param name="Id">The publication.</param>
[InvalidatesCache(CacheTags.Publications)]
public sealed record DeletePublicationCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeletePublicationCommand"/>.</summary>
public sealed class DeletePublicationCommandHandler(IPublicationRepository publications, IUnitOfWork unitOfWork,
                                                    TimeProvider clock)
    : IRequestHandler<DeletePublicationCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(DeletePublicationCommand c, CancellationToken ct) =>
        ContentActions.DeleteAsync(publications, unitOfWork, clock, c.Id, PublicationErrors.NotFound, ct);
}

/// <summary>Admin → Publications: drag-to-reorder. Send every publication's Id in the new order.</summary>
/// <param name="Ids">Every publication's Id, each once.</param>
[InvalidatesCache(CacheTags.Publications)]
public sealed record ReorderPublicationsCommand(IReadOnlyList<Guid> Ids) : IRequest<Result>;

/// <summary>Handles <see cref="ReorderPublicationsCommand"/>.</summary>
public sealed class ReorderPublicationsCommandHandler(IPublicationRepository publications, IUnitOfWork unitOfWork)
    : IRequestHandler<ReorderPublicationsCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ReorderPublicationsCommand c, CancellationToken ct) =>
        ContentActions.ReorderAsync(publications, unitOfWork, c.Ids, ct);
}

/// <summary>Turns a command's fields into the Domain's <see cref="PublicationDetails"/>.</summary>
internal static class PublicationMapping
{
    public static PublicationDetails Details(string title, string authors, string venue, int year,
                                             PublicationType type, string? award,
                                             IReadOnlyList<PublicationLinkDto>? links) =>
        new(title, authors, venue, year, type, award,
            (links ?? []).Select(l => new PublicationLinkDetails(l.Label, l.Url, l.IsPrimary)).ToList());
}
