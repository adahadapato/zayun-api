using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.CareerRoles.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.CareerRoles.Queries;

/// <summary>The website's Experience page ("Roles") and the CVs: published roles, current first, then newest.</summary>
[Cacheable(CacheTags.CareerRoles, Minutes = 60)]
public sealed record GetCareerRolesQuery : IRequest<Result<IReadOnlyList<CareerRoleDto>>>;

/// <summary>Handles <see cref="GetCareerRolesQuery"/>.</summary>
public sealed class GetCareerRolesQueryHandler(ICareerRoleRepository roles)
    : IRequestHandler<GetCareerRolesQuery, Result<IReadOnlyList<CareerRoleDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CareerRoleDto>>> Handle(GetCareerRolesQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<CareerRoleDto>>(
            CareerRoleOrder.Sort(await roles.GetListAsync(publishedOnly: true, ct)).Select(CareerRoleDto.From).ToList());
}

/// <summary>Admin → Roles: every role, drafts included, in the same order as the website. Never cached.</summary>
public sealed record GetAdminCareerRolesQuery : IRequest<Result<IReadOnlyList<CareerRoleDto>>>;

/// <summary>Handles <see cref="GetAdminCareerRolesQuery"/>.</summary>
public sealed class GetAdminCareerRolesQueryHandler(ICareerRoleRepository roles)
    : IRequestHandler<GetAdminCareerRolesQuery, Result<IReadOnlyList<CareerRoleDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CareerRoleDto>>> Handle(GetAdminCareerRolesQuery query,
                                                                    CancellationToken ct) =>
        Result.Success<IReadOnlyList<CareerRoleDto>>(
            CareerRoleOrder.Sort(await roles.GetListAsync(publishedOnly: false, ct)).Select(CareerRoleDto.From).ToList());
}

/// <summary>
/// The order roles are shown in, everywhere: current roles first, then by end date (newest first), then by start date
/// (newest first). A missing month counts as the end of the year for an end and the start of the year for a start.
/// </summary>
public static class CareerRoleOrder
{
    /// <summary>Sorts the roles.</summary>
    /// <param name="roles">The roles.</param>
    /// <returns>The roles in order.</returns>
    public static IEnumerable<CareerRole> Sort(IEnumerable<CareerRole> roles) => roles
        .OrderBy(r => r.IsCurrent ? 0 : 1)
        .ThenByDescending(r => r.EndYear is { } y ? y * 100 + (r.EndMonth ?? 12) : int.MaxValue)
        .ThenByDescending(r => r.StartYear * 100 + (r.StartMonth ?? 1))
        .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase);
}
