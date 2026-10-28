using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Projects.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Features.Projects.Commands;

/// <summary>Admin → Edit project: changes everything except its place, published state and pictures.</summary>
/// <param name="Id">The project.</param>
/// <param name="Title">The project's name.</param>
/// <param name="Slug">Its web address; empty = made from the title.</param>
/// <param name="Subtitle">One line under the title.</param>
/// <param name="Description">What it is and does.</param>
/// <param name="Category">Featured or Engineering.</param>
/// <param name="Label">The small label on the card.</param>
/// <param name="VisualLabel">Optional label over the picture area.</param>
/// <param name="GithubUrl">Optional link to the source code.</param>
/// <param name="LiveUrl">Optional link to the live site.</param>
/// <param name="IsPrivateRepo">True when the code is private.</param>
/// <param name="Tags">Technologies and topics.</param>
[InvalidatesCache(CacheTags.Projects)]
public sealed record UpdateProjectCommand(
    Guid Id,
    string Title,
    string? Slug,
    string Subtitle,
    string Description,
    ProjectCategory Category,
    string Label,
    string? VisualLabel,
    string? GithubUrl,
    string? LiveUrl,
    bool IsPrivateRepo,
    IReadOnlyList<string>? Tags) : IRequest<Result<ProjectDto>>;

/// <summary>Handles <see cref="UpdateProjectCommand"/>.</summary>
public sealed class UpdateProjectCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork,
                                                IFileStorage storage)
    : IRequestHandler<UpdateProjectCommand, Result<ProjectDto>>
{
    /// <inheritdoc />
    public async Task<Result<ProjectDto>> Handle(UpdateProjectCommand command, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(command.Id, ct);
        if (project is null) return ProjectErrors.NotFound;

        var slug = Slugs.From(string.IsNullOrWhiteSpace(command.Slug) ? command.Title : command.Slug);
        if (slug.Length == 0) return ProjectErrors.SlugEmpty;
        if (slug != project.Slug && await projects.SlugExistsAsync(slug, project.Id, ct))
            return ProjectErrors.SlugTaken;

        project.Update(
            new ProjectDetails(command.Title, command.Subtitle, command.Description, command.Category, command.Label,
                               command.VisualLabel, command.GithubUrl, command.LiveUrl, command.IsPrivateRepo,
                               command.Tags ?? []),
            slug);

        await unitOfWork.SaveChangesAsync(ct);
        return ProjectDto.From(project, storage);
    }
}
