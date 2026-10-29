using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Awards.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Awards.Queries;

/// <summary>
/// The website's Awards page (and the highlights on About and Experience, which use the featured ones): published
/// awards in order.
/// </summary>
[Cacheable(CacheTags.Awards, Minutes = 60)]
public sealed record GetAwardsQuery : IRequest<Result<IReadOnlyList<AwardDto>>>;

/// <summary>Handles <see cref="GetAwardsQuery"/>.</summary>
public sealed class GetAwardsQueryHandler(IAwardRepository awards, IFileStorage storage)
    : IRequestHandler<GetAwardsQuery, Result<IReadOnlyList<AwardDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AwardDto>>> Handle(GetAwardsQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<AwardDto>>(
            (await awards.GetListAsync(publishedOnly: true, ct)).Select(a => AwardDto.From(a, storage)).ToList());
}

/// <summary>Admin → Awards: every award, drafts included. Never cached.</summary>
public sealed record GetAdminAwardsQuery : IRequest<Result<IReadOnlyList<AwardDto>>>;

/// <summary>Handles <see cref="GetAdminAwardsQuery"/>.</summary>
public sealed class GetAdminAwardsQueryHandler(IAwardRepository awards, IFileStorage storage)
    : IRequestHandler<GetAdminAwardsQuery, Result<IReadOnlyList<AwardDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AwardDto>>> Handle(GetAdminAwardsQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<AwardDto>>(
            (await awards.GetListAsync(publishedOnly: false, ct)).Select(a => AwardDto.From(a, storage)).ToList());
}
