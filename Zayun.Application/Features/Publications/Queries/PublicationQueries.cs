using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Publications.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Publications.Queries;

/// <summary>The website's Research page: published publications in order.</summary>
[Cacheable(CacheTags.Publications, Minutes = 60)]
public sealed record GetPublicationsQuery : IRequest<Result<IReadOnlyList<PublicationDto>>>;

/// <summary>Handles <see cref="GetPublicationsQuery"/>.</summary>
public sealed class GetPublicationsQueryHandler(IPublicationRepository publications)
    : IRequestHandler<GetPublicationsQuery, Result<IReadOnlyList<PublicationDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PublicationDto>>> Handle(GetPublicationsQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<PublicationDto>>(
            (await publications.GetListAsync(publishedOnly: true, ct)).Select(PublicationDto.From).ToList());
}

/// <summary>Admin → Publications: every publication, drafts included. Never cached.</summary>
public sealed record GetAdminPublicationsQuery : IRequest<Result<IReadOnlyList<PublicationDto>>>;

/// <summary>Handles <see cref="GetAdminPublicationsQuery"/>.</summary>
public sealed class GetAdminPublicationsQueryHandler(IPublicationRepository publications)
    : IRequestHandler<GetAdminPublicationsQuery, Result<IReadOnlyList<PublicationDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PublicationDto>>> Handle(GetAdminPublicationsQuery query,
                                                                     CancellationToken ct) =>
        Result.Success<IReadOnlyList<PublicationDto>>(
            (await publications.GetListAsync(publishedOnly: false, ct)).Select(PublicationDto.From).ToList());
}
