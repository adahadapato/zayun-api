using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Content;
using Zayun.Application.Features.Awards.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Awards.Commands;

/// <summary>Admin → New award (add its picture afterwards). It goes to the end of the list.</summary>
/// <param name="Title">What it's called.</param>
/// <param name="AwardedBy">Who gave it, or the event.</param>
/// <param name="Year">The year.</param>
/// <param name="Description">What it was for.</param>
/// <param name="Url">Where to read more (optional).</param>
/// <param name="IsFeatured">Also show it on the About and Experience pages.</param>
/// <param name="IsPublished">Show it straight away, or keep it as a draft.</param>
[InvalidatesCache(CacheTags.Awards)]
public sealed record CreateAwardCommand(
    string Title,
    string AwardedBy,
    int Year,
    string Description,
    string? Url,
    bool IsFeatured,
    bool IsPublished) : IRequest<Result<AwardDto>>;

/// <summary>Handles <see cref="CreateAwardCommand"/>.</summary>
public sealed class CreateAwardCommandHandler(IAwardRepository awards, IUnitOfWork unitOfWork, IFileStorage storage)
    : IRequestHandler<CreateAwardCommand, Result<AwardDto>>
{
    /// <inheritdoc />
    public async Task<Result<AwardDto>> Handle(CreateAwardCommand c, CancellationToken ct)
    {
        var award = Award.Create(
            new AwardDetails(c.Title, c.AwardedBy, c.Year, c.Description, c.Url, c.IsFeatured, ImageAlt: null),
            await awards.GetNextDisplayOrderAsync(ct),
            c.IsPublished);

        awards.Add(award);
        await unitOfWork.SaveChangesAsync(ct);
        return AwardDto.From(award, storage);
    }
}

/// <summary>Admin → Edit award: everything except its place, published state and picture file.</summary>
/// <param name="Id">The award.</param>
/// <param name="Title">What it's called.</param>
/// <param name="AwardedBy">Who gave it, or the event.</param>
/// <param name="Year">The year.</param>
/// <param name="Description">What it was for.</param>
/// <param name="Url">Where to read more (optional).</param>
/// <param name="IsFeatured">Also show it on the About and Experience pages.</param>
/// <param name="ImageAlt">New alt text for the picture; null leaves it as it is.</param>
[InvalidatesCache(CacheTags.Awards)]
public sealed record UpdateAwardCommand(
    Guid Id,
    string Title,
    string AwardedBy,
    int Year,
    string Description,
    string? Url,
    bool IsFeatured,
    string? ImageAlt) : IRequest<Result<AwardDto>>;

/// <summary>Handles <see cref="UpdateAwardCommand"/>.</summary>
public sealed class UpdateAwardCommandHandler(IAwardRepository awards, IUnitOfWork unitOfWork, IFileStorage storage)
    : IRequestHandler<UpdateAwardCommand, Result<AwardDto>>
{
    /// <inheritdoc />
    public async Task<Result<AwardDto>> Handle(UpdateAwardCommand c, CancellationToken ct)
    {
        var award = await awards.GetByIdAsync(c.Id, ct);
        if (award is null) return AwardErrors.NotFound;

        award.Update(new AwardDetails(c.Title, c.AwardedBy, c.Year, c.Description, c.Url, c.IsFeatured,
                                      string.IsNullOrWhiteSpace(c.ImageAlt) ? null : c.ImageAlt));
        await unitOfWork.SaveChangesAsync(ct);
        return AwardDto.From(award, storage);
    }
}

/// <summary>Admin → Awards: show or hide an award.</summary>
/// <param name="Id">The award.</param>
/// <param name="IsPublished">True to show it.</param>
[InvalidatesCache(CacheTags.Awards)]
public sealed record SetAwardPublishedCommand(Guid Id, bool IsPublished) : IRequest<Result>;

/// <summary>Handles <see cref="SetAwardPublishedCommand"/>.</summary>
public sealed class SetAwardPublishedCommandHandler(IAwardRepository awards, IUnitOfWork unitOfWork)
    : IRequestHandler<SetAwardPublishedCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(SetAwardPublishedCommand c, CancellationToken ct) =>
        ContentActions.SetPublishedAsync(awards, unitOfWork, c.Id, c.IsPublished, AwardErrors.NotFound, ct);
}

/// <summary>Admin → Awards: delete an award (soft delete; its picture files are kept).</summary>
/// <param name="Id">The award.</param>
[InvalidatesCache(CacheTags.Awards)]
public sealed record DeleteAwardCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteAwardCommand"/>.</summary>
public sealed class DeleteAwardCommandHandler(IAwardRepository awards, IUnitOfWork unitOfWork, TimeProvider clock)
    : IRequestHandler<DeleteAwardCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(DeleteAwardCommand c, CancellationToken ct) =>
        ContentActions.DeleteAsync(awards, unitOfWork, clock, c.Id, AwardErrors.NotFound, ct);
}

/// <summary>Admin → Awards: drag-to-reorder. Send every award's Id in the new order.</summary>
/// <param name="Ids">Every award's Id, each once.</param>
[InvalidatesCache(CacheTags.Awards)]
public sealed record ReorderAwardsCommand(IReadOnlyList<Guid> Ids) : IRequest<Result>;

/// <summary>Handles <see cref="ReorderAwardsCommand"/>.</summary>
public sealed class ReorderAwardsCommandHandler(IAwardRepository awards, IUnitOfWork unitOfWork)
    : IRequestHandler<ReorderAwardsCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ReorderAwardsCommand c, CancellationToken ct) =>
        ContentActions.ReorderAsync(awards, unitOfWork, c.Ids, ct);
}
