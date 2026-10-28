using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Projects.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Projects.Queries;

/// <summary>One published project by its web address (a project's own page).</summary>
/// <param name="Slug">The web address, e.g. "rccg-hope-house".</param>
[Cacheable(CacheTags.Projects, Minutes = 60)]
public sealed record GetProjectBySlugQuery(string Slug) : IRequest<Result<ProjectDto>>;

/// <summary>Handles <see cref="GetProjectBySlugQuery"/>.</summary>
public sealed class GetProjectBySlugQueryHandler(IProjectRepository projects, IFileStorage storage)
    : IRequestHandler<GetProjectBySlugQuery, Result<ProjectDto>>
{
    /// <inheritdoc />
    public async Task<Result<ProjectDto>> Handle(GetProjectBySlugQuery query, CancellationToken ct)
    {
        var project = await projects.GetBySlugAsync(query.Slug.Trim().ToLowerInvariant(), publishedOnly: true, ct);
        return project is null ? ProjectErrors.NotFound : ProjectDto.From(project, storage);
    }
}
