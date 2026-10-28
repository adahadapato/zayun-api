using MediatR;
using Zayun.Application.Features.Projects.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Projects.Queries;

/// <summary>Admin → Projects: every project, drafts included, in order. Never cached.</summary>
public sealed record GetAdminProjectsQuery : IRequest<Result<IReadOnlyList<ProjectDto>>>;

/// <summary>Handles <see cref="GetAdminProjectsQuery"/>.</summary>
public sealed class GetAdminProjectsQueryHandler(IProjectRepository projects, IFileStorage storage)
    : IRequestHandler<GetAdminProjectsQuery, Result<IReadOnlyList<ProjectDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProjectDto>>> Handle(GetAdminProjectsQuery query, CancellationToken ct)
    {
        var list = await projects.GetListAsync(category: null, publishedOnly: false, ct);
        return Result.Success<IReadOnlyList<ProjectDto>>(list.Select(p => ProjectDto.From(p, storage)).ToList());
    }
}

/// <summary>Admin → Edit project: one project, draft or not.</summary>
/// <param name="Id">The project.</param>
public sealed record GetAdminProjectQuery(Guid Id) : IRequest<Result<ProjectDto>>;

/// <summary>Handles <see cref="GetAdminProjectQuery"/>.</summary>
public sealed class GetAdminProjectQueryHandler(IProjectRepository projects, IFileStorage storage)
    : IRequestHandler<GetAdminProjectQuery, Result<ProjectDto>>
{
    /// <inheritdoc />
    public async Task<Result<ProjectDto>> Handle(GetAdminProjectQuery query, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(query.Id, ct);
        return project is null ? ProjectErrors.NotFound : ProjectDto.From(project, storage);
    }
}
