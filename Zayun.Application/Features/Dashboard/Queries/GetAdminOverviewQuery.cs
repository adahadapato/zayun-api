using MediatR;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Dashboard.Queries;

/// <summary>Admin → Dashboard: how much content there is (and how much is published), and new messages.</summary>
public sealed record GetAdminOverviewQuery : IRequest<Result<AdminOverviewDto>>;

/// <summary>The dashboard's numbers.</summary>
/// <param name="Projects">Projects.</param>
/// <param name="Publications">Publications.</param>
/// <param name="Presentations">Presentations.</param>
/// <param name="ExperienceAreas">Experience areas.</param>
/// <param name="SkillGroups">Skill groups.</param>
/// <param name="CoreExpertise">Core expertise items.</param>
/// <param name="NewMessages">Contact messages not looked at yet.</param>
/// <param name="Documents">Private documents stored.</param>
public sealed record AdminOverviewDto(
    ContentCountDto Projects,
    ContentCountDto Publications,
    ContentCountDto Presentations,
    ContentCountDto ExperienceAreas,
    ContentCountDto SkillGroups,
    ContentCountDto CoreExpertise,
    int NewMessages,
    int Documents);

/// <summary>How many items, and how many of them the website shows.</summary>
/// <param name="Total">All items, drafts included.</param>
/// <param name="Published">Items shown on the website.</param>
public sealed record ContentCountDto(int Total, int Published);

/// <summary>Handles <see cref="GetAdminOverviewQuery"/>.</summary>
/// <remarks>A portfolio holds tens of items, not thousands, so counting the loaded lists is plenty.</remarks>
public sealed class GetAdminOverviewQueryHandler(
    IProjectRepository projects,
    IPublicationRepository publications,
    IPresentationRepository presentations,
    IExperienceAreaRepository experience,
    ISkillGroupRepository skillGroups,
    ICoreExpertiseRepository expertise,
    IContactMessageRepository messages,
    IDocumentRepository documents)
    : IRequestHandler<GetAdminOverviewQuery, Result<AdminOverviewDto>>
{
    /// <inheritdoc />
    public async Task<Result<AdminOverviewDto>> Handle(GetAdminOverviewQuery query, CancellationToken ct)
    {
        // One after the other: all repositories share one DbContext.
        var projectCount = Count(await projects.GetListAsync(publishedOnly: false, ct));
        var publicationCount = Count(await publications.GetListAsync(publishedOnly: false, ct));
        var presentationCount = Count(await presentations.GetListAsync(publishedOnly: false, ct));
        var experienceCount = Count(await experience.GetListAsync(publishedOnly: false, ct));
        var groupCount = Count(await skillGroups.GetListAsync(publishedOnly: false, ct));
        var expertiseCount = Count(await expertise.GetListAsync(publishedOnly: false, ct));
        var byStatus = await messages.CountByStatusAsync(ct);
        var documentCount = await documents.CountAsync(ct);

        return new AdminOverviewDto(projectCount, publicationCount, presentationCount, experienceCount, groupCount,
                                    expertiseCount, byStatus.GetValueOrDefault(ContactMessageStatus.New),
                                    documentCount);
    }

    private static ContentCountDto Count<T>(IReadOnlyList<T> items) where T : ContentEntity =>
        new(items.Count, items.Count(i => i.IsPublished));
}
