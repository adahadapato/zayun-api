using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;

namespace Zayun.Application.Features.Projects.Dtos;

/// <summary>A project as the website and the admin area show it.</summary>
/// <param name="Id">The project's Id.</param>
/// <param name="Slug">Its web address.</param>
/// <param name="Title">Its name.</param>
/// <param name="Subtitle">One line under the title.</param>
/// <param name="Description">What it is and does.</param>
/// <param name="Category">Featured or Engineering.</param>
/// <param name="Label">The small label on the card.</param>
/// <param name="VisualLabel">Label over the picture area, if any.</param>
/// <param name="GithubUrl">The source code, if public.</param>
/// <param name="LiveUrl">The live site, if any.</param>
/// <param name="IsPrivateRepo">True when the code is private.</param>
/// <param name="Tags">Technologies and topics.</param>
/// <param name="Images">Its pictures, in order.</param>
/// <param name="DisplayOrder">Its place in the list.</param>
/// <param name="IsPublished">False for a draft (only the admin area sees drafts).</param>
/// <param name="UpdatedAt">When it last changed (UTC), or when it was created.</param>
public sealed record ProjectDto(
    Guid Id,
    string Slug,
    string Title,
    string Subtitle,
    string Description,
    ProjectCategory Category,
    string Label,
    string? VisualLabel,
    string? GithubUrl,
    string? LiveUrl,
    bool IsPrivateRepo,
    IReadOnlyList<string> Tags,
    IReadOnlyList<ProjectImageDto> Images,
    int DisplayOrder,
    bool IsPublished,
    DateTime UpdatedAt)
{
    /// <summary>Maps a project, with absolute picture links.</summary>
    /// <param name="project">The project (with its pictures loaded).</param>
    /// <param name="storage">Turns stored paths into links.</param>
    /// <returns>The DTO.</returns>
    public static ProjectDto From(Project project, IFileStorage storage) =>
        new(project.Id, project.Slug, project.Title, project.Subtitle, project.Description, project.Category,
            project.Label, project.VisualLabel, project.GithubUrl, project.LiveUrl, project.IsPrivateRepo,
            project.Tags.ToList(),
            project.Images.OrderBy(i => i.DisplayOrder).Select(i => ProjectImageDto.From(i, storage)).ToList(),
            project.DisplayOrder, project.IsPublished, project.UpdatedAt ?? project.CreatedAt);
}

/// <summary>One project picture.</summary>
/// <param name="Id">The picture's Id.</param>
/// <param name="Url">The full-size picture.</param>
/// <param name="ThumbnailUrl">The small version, if any.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="AltText">Text for screen readers.</param>
/// <param name="Label">Caption, if any.</param>
/// <param name="Kind">Primary, Secondary or Gallery.</param>
/// <param name="DisplayOrder">Its place among the project's pictures.</param>
public sealed record ProjectImageDto(
    Guid Id,
    string Url,
    string? ThumbnailUrl,
    int Width,
    int Height,
    string AltText,
    string? Label,
    ProjectImageKind Kind,
    int DisplayOrder)
{
    /// <summary>Maps a picture, with absolute links.</summary>
    /// <param name="image">The picture.</param>
    /// <param name="storage">Turns stored paths into links.</param>
    /// <returns>The DTO.</returns>
    public static ProjectImageDto From(ProjectImage image, IFileStorage storage) =>
        new(image.Id,
            storage.GetPublicUrl(image.File.Path),
            image.File.ThumbnailPath is null ? null : storage.GetPublicUrl(image.File.ThumbnailPath),
            image.File.Dimensions.Width, image.File.Dimensions.Height,
            image.AltText, image.Label, image.Kind, image.DisplayOrder);
}
