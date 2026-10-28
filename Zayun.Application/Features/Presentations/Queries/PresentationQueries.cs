using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Presentations.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Presentations.Queries;

/// <summary>The website's Research page: published presentations in order.</summary>
[Cacheable(CacheTags.Presentations, Minutes = 60)]
public sealed record GetPresentationsQuery : IRequest<Result<IReadOnlyList<PresentationDto>>>;

/// <summary>Handles <see cref="GetPresentationsQuery"/>.</summary>
public sealed class GetPresentationsQueryHandler(IPresentationRepository presentations, IFileStorage storage)
    : IRequestHandler<GetPresentationsQuery, Result<IReadOnlyList<PresentationDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PresentationDto>>> Handle(GetPresentationsQuery query,
                                                                      CancellationToken ct) =>
        Result.Success<IReadOnlyList<PresentationDto>>(
            (await presentations.GetListAsync(publishedOnly: true, ct))
                .Select(p => PresentationDto.From(p, storage)).ToList());
}

/// <summary>Admin → Presentations: every presentation, drafts included. Never cached.</summary>
public sealed record GetAdminPresentationsQuery : IRequest<Result<IReadOnlyList<PresentationDto>>>;

/// <summary>Handles <see cref="GetAdminPresentationsQuery"/>.</summary>
public sealed class GetAdminPresentationsQueryHandler(IPresentationRepository presentations, IFileStorage storage)
    : IRequestHandler<GetAdminPresentationsQuery, Result<IReadOnlyList<PresentationDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PresentationDto>>> Handle(GetAdminPresentationsQuery query,
                                                                      CancellationToken ct) =>
        Result.Success<IReadOnlyList<PresentationDto>>(
            (await presentations.GetListAsync(publishedOnly: false, ct))
                .Select(p => PresentationDto.From(p, storage)).ToList());
}
