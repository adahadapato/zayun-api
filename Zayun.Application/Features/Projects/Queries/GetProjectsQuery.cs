using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Projects.Dtos;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Projects.Queries;

/// <summary>The website's Projects page: published projects in order, optionally one category.</summary>
/// <param name="Category">Only this category; null = both.</param>
[Cacheable(CacheTags.Projects, Minutes = 60)]
public sealed record GetProjectsQuery(ProjectCategory? Category = null)
    : IRequest<Result<IReadOnlyList<ProjectDto>>>;

/// <summary>Handles <see cref="GetProjectsQuery"/>.</summary>
public sealed class GetProjectsQueryHandler(IProjectRepository projects, IFileStorage storage)
    : IRequestHandler<GetProjectsQuery, Result<IReadOnlyList<ProjectDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProjectDto>>> Handle(GetProjectsQuery query, CancellationToken ct)
    {
        var list = await projects.GetListAsync(query.Category, publishedOnly: true, ct);
        return Result.Success<IReadOnlyList<ProjectDto>>(list.Select(p => ProjectDto.From(p, storage)).ToList());
    }
}
