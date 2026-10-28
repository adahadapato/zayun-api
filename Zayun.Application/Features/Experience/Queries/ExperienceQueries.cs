using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Experience.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Experience.Queries;

/// <summary>The website's Experience page: published areas in order.</summary>
[Cacheable(CacheTags.Experience, Minutes = 60)]
public sealed record GetExperienceQuery : IRequest<Result<IReadOnlyList<ExperienceAreaDto>>>;

/// <summary>Handles <see cref="GetExperienceQuery"/>.</summary>
public sealed class GetExperienceQueryHandler(IExperienceAreaRepository areas)
    : IRequestHandler<GetExperienceQuery, Result<IReadOnlyList<ExperienceAreaDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ExperienceAreaDto>>> Handle(GetExperienceQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<ExperienceAreaDto>>(
            (await areas.GetListAsync(publishedOnly: true, ct)).Select(ExperienceAreaDto.From).ToList());
}

/// <summary>Admin → Experience: every area, drafts included. Never cached.</summary>
public sealed record GetAdminExperienceQuery : IRequest<Result<IReadOnlyList<ExperienceAreaDto>>>;

/// <summary>Handles <see cref="GetAdminExperienceQuery"/>.</summary>
public sealed class GetAdminExperienceQueryHandler(IExperienceAreaRepository areas)
    : IRequestHandler<GetAdminExperienceQuery, Result<IReadOnlyList<ExperienceAreaDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ExperienceAreaDto>>> Handle(GetAdminExperienceQuery query,
                                                                        CancellationToken ct) =>
        Result.Success<IReadOnlyList<ExperienceAreaDto>>(
            (await areas.GetListAsync(publishedOnly: false, ct)).Select(ExperienceAreaDto.From).ToList());
}
