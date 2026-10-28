using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Skills.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Skills.Queries;

/// <summary>The website's Skills page: published skill groups and core expertise, in order.</summary>
[Cacheable(CacheTags.Skills, Minutes = 60)]
public sealed record GetSkillsQuery : IRequest<Result<SkillsPageDto>>;

/// <summary>Handles <see cref="GetSkillsQuery"/>.</summary>
public sealed class GetSkillsQueryHandler(ISkillGroupRepository groups, ICoreExpertiseRepository expertise)
    : IRequestHandler<GetSkillsQuery, Result<SkillsPageDto>>
{
    /// <inheritdoc />
    public async Task<Result<SkillsPageDto>> Handle(GetSkillsQuery query, CancellationToken ct) =>
        await SkillsPage.LoadAsync(groups, expertise, publishedOnly: true, ct);
}

/// <summary>Admin → Skills: every group and core expertise item, drafts included. Never cached.</summary>
public sealed record GetAdminSkillsQuery : IRequest<Result<SkillsPageDto>>;

/// <summary>Handles <see cref="GetAdminSkillsQuery"/>.</summary>
public sealed class GetAdminSkillsQueryHandler(ISkillGroupRepository groups, ICoreExpertiseRepository expertise)
    : IRequestHandler<GetAdminSkillsQuery, Result<SkillsPageDto>>
{
    /// <inheritdoc />
    public async Task<Result<SkillsPageDto>> Handle(GetAdminSkillsQuery query, CancellationToken ct) =>
        await SkillsPage.LoadAsync(groups, expertise, publishedOnly: false, ct);
}

/// <summary>Loads both lists of the Skills page.</summary>
internal static class SkillsPage
{
    public static async Task<Result<SkillsPageDto>> LoadAsync(ISkillGroupRepository groups,
                                                              ICoreExpertiseRepository expertise,
                                                              bool publishedOnly, CancellationToken ct)
    {
        // One after the other: both repositories share one DbContext, which can't run two queries at once.
        var groupList = await groups.GetListAsync(publishedOnly, ct);
        var expertiseList = await expertise.GetListAsync(publishedOnly, ct);

        return new SkillsPageDto(groupList.Select(SkillGroupDto.From).ToList(),
                                 expertiseList.Select(CoreExpertiseDto.From).ToList());
    }
}
