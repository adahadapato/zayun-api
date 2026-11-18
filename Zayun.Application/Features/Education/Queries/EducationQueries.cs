using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Education.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Education.Queries;

/// <summary>The website's Experience page ("Education") and the CVs: published qualifications, in progress first, then
/// newest.</summary>
[Cacheable(CacheTags.Education, Minutes = 60)]
public sealed record GetEducationQuery : IRequest<Result<IReadOnlyList<QualificationDto>>>;

/// <summary>Handles <see cref="GetEducationQuery"/>.</summary>
public sealed class GetEducationQueryHandler(IQualificationRepository qualifications)
    : IRequestHandler<GetEducationQuery, Result<IReadOnlyList<QualificationDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<QualificationDto>>> Handle(GetEducationQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<QualificationDto>>(
            QualificationOrder.Sort(await qualifications.GetListAsync(publishedOnly: true, ct))
                              .Select(QualificationDto.From).ToList());
}

/// <summary>Admin → Education: every qualification, drafts included, in the same order as the website. Never cached.</summary>
public sealed record GetAdminEducationQuery : IRequest<Result<IReadOnlyList<QualificationDto>>>;

/// <summary>Handles <see cref="GetAdminEducationQuery"/>.</summary>
public sealed class GetAdminEducationQueryHandler(IQualificationRepository qualifications)
    : IRequestHandler<GetAdminEducationQuery, Result<IReadOnlyList<QualificationDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<QualificationDto>>> Handle(GetAdminEducationQuery query,
                                                                       CancellationToken ct) =>
        Result.Success<IReadOnlyList<QualificationDto>>(
            QualificationOrder.Sort(await qualifications.GetListAsync(publishedOnly: false, ct))
                              .Select(QualificationDto.From).ToList());
}

/// <summary>The order qualifications are shown in, everywhere: in progress first, then by year (newest first).</summary>
public static class QualificationOrder
{
    /// <summary>Sorts the qualifications.</summary>
    /// <param name="qualifications">The qualifications.</param>
    /// <returns>The qualifications in order.</returns>
    public static IEnumerable<Qualification> Sort(IEnumerable<Qualification> qualifications) => qualifications
        .OrderBy(q => q.IsInProgress ? 0 : 1)
        .ThenByDescending(q => q.Year ?? int.MaxValue)
        .ThenBy(q => q.Degree, StringComparer.OrdinalIgnoreCase);
}
