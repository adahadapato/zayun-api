using MediatR;
using Microsoft.Extensions.Logging;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Projects.Dtos;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Projects.Commands;

/// <summary>Admin → Edit project → Add picture. The upload is resized and converted to WebP, then added at the end of
/// the project's pictures.</summary>
/// <param name="ProjectId">The project.</param>
/// <param name="Content">The uploaded file.</param>
/// <param name="SizeBytes">Its size, as sent.</param>
/// <param name="AltText">Text for screen readers.</param>
/// <param name="Label">Optional caption.</param>
/// <param name="Kind">Primary, Secondary or Gallery.</param>
[InvalidatesCache(CacheTags.Projects)]
public sealed record AddProjectImageCommand(
    Guid ProjectId,
    Stream Content,
    long SizeBytes,
    string AltText,
    string? Label,
    ProjectImageKind Kind) : IRequest<Result<ProjectImageDto>>;

/// <summary>Handles <see cref="AddProjectImageCommand"/>.</summary>
/// <remarks>The files are saved before the row. If the project refuses the picture or the save fails, the files are
/// deleted again, so storage never fills with orphans.</remarks>
public sealed class AddProjectImageCommandHandler(
    IProjectRepository projects,
    IUnitOfWork unitOfWork,
    IImageProcessor images,
    IFileStorage storage,
    ILogger<AddProjectImageCommandHandler> logger)
    : IRequestHandler<AddProjectImageCommand, Result<ProjectImageDto>>
{
    /// <inheritdoc />
    public async Task<Result<ProjectImageDto>> Handle(AddProjectImageCommand command, CancellationToken ct)
    {
        // 1. The project
        var project = await projects.GetByIdAsync(command.ProjectId, ct);
        if (project is null) return ProjectErrors.NotFound;
        if (project.Images.Count >= Domain.Entities.Project.MaxImages) return ProjectErrors.TooManyImages;

        // 2. The picture: resized, WebP, metadata stripped
        var processed = await images.ProcessAsync(command.Content, ct);
        if (processed.IsFailure) return processed.Error;

        var imageId = Guid.CreateVersion7();
        Domain.ValueObjects.ImageFile file;
        await using (var image = processed.Value)
        {
            file = await storage.SaveImageAsync(image, $"projects/{project.Id:N}", imageId, ct);
        }

        // 3. On the project, then saved
        try
        {
            var added = project.AddImage(imageId, file, command.AltText, command.Label, command.Kind);
            if (added.IsFailure)
            {
                await DeleteQuietlyAsync(file);
                return added.Error;
            }

            await unitOfWork.SaveChangesAsync(ct);
            return ProjectImageDto.From(added.Value, storage);
        }
        catch
        {
            await DeleteQuietlyAsync(file);
            throw;
        }
    }

    /// <summary>Removes files that were stored but won't be used; a failure here is only logged.</summary>
    private async Task DeleteQuietlyAsync(Domain.ValueObjects.ImageFile file)
    {
        try
        {
            await storage.DeleteImageAsync(file, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't delete unused picture files {Path}.", file.Path);
        }
    }
}
