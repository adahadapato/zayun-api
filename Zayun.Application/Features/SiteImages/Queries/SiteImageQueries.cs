using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.SiteImages.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.SiteImages.Queries;

/// <summary>The website and the admin area: every named picture (empty slots are left out).</summary>
[Cacheable(CacheTags.SiteImages, Minutes = 60)]
public sealed record GetSiteImagesQuery : IRequest<Result<IReadOnlyList<SiteImageDto>>>;

/// <summary>Handles <see cref="GetSiteImagesQuery"/>.</summary>
public sealed class GetSiteImagesQueryHandler(ISiteImageRepository images, IFileStorage storage)
    : IRequestHandler<GetSiteImagesQuery, Result<IReadOnlyList<SiteImageDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SiteImageDto>>> Handle(GetSiteImagesQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<SiteImageDto>>(
            (await images.GetAllAsync(ct)).Select(i => SiteImageDto.From(i, storage)).ToList());
}
