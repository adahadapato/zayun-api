using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Tags;
using Zayun.Application.Features.Projects.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Features.Projects.Commands;

/// <summary>Admin → New project. It goes to the end of the list; add pictures afterwards.</summary>
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
/// <param name="IsPublished">Show it straight away, or keep it as a draft.</param>
[InvalidatesCache(CacheTags.Projects)]
public sealed record CreateProjectCommand(
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
    IReadOnlyList<string>? Tags,
    bool IsPublished) : IRequest<Result<ProjectDto>>;

/// <summary>Handles <see cref="CreateProjectCommand"/>.</summary>
public sealed class CreateProjectCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork,
                                                IFileStorage storage, TagResolver tags)
    : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
{
    /// <inheritdoc />
    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand command, CancellationToken ct)
    {
        var slug = Slugs.From(string.IsNullOrWhiteSpace(command.Slug) ? command.Title : command.Slug);
        if (slug.Length == 0) return ProjectErrors.SlugEmpty;
        if (await projects.SlugExistsAsync(slug, exceptProjectId: null, ct)) return ProjectErrors.SlugTaken;

        var project = Project.Create(
            new ProjectDetails(command.Title, command.Subtitle, command.Description, command.Category, command.Label,
                               command.VisualLabel, command.GithubUrl, command.LiveUrl, command.IsPrivateRepo,
                               await tags.ResolveAsync(command.Tags, ct)),
            slug,
            await projects.GetNextDisplayOrderAsync(ct),
            command.IsPublished);

        projects.Add(project);
        await unitOfWork.SaveChangesAsync(ct);
        return ProjectDto.From(project, storage);
    }
}
