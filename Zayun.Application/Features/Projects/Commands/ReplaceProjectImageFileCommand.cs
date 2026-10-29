using MediatR;
using Microsoft.Extensions.Logging;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Projects.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.ValueObjects;

namespace Zayun.Application.Features.Projects.Commands;

/// <summary>
/// Admin → Edit project → Edit picture → Change picture: swaps the file of an existing picture. The picture keeps its
/// place, alt text, caption and kind. The upload is resized and converted to WebP like any other.
/// </summary>
/// <param name="ProjectId">The project.</param>
/// <param name="ImageId">The picture.</param>
/// <param name="Content">The uploaded file.</param>
/// <param name="SizeBytes">Its size, as sent.</param>
[InvalidatesCache(CacheTags.Projects)]
public sealed record ReplaceProjectImageFileCommand(Guid ProjectId, Guid ImageId, Stream Content, long SizeBytes)
    : IRequest<Result<ProjectImageDto>>;

/// <summary>Handles <see cref="ReplaceProjectImageFileCommand"/>.</summary>
/// <remarks>
/// The new files get a new name (so browsers never show the old cached picture). They're saved first; the old ones are
/// deleted only after the change is saved, and the new ones are deleted again if anything fails.
/// </remarks>
public sealed class ReplaceProjectImageFileCommandHandler(
    IProjectRepository projects,
    IUnitOfWork unitOfWork,
    IImageProcessor images,
    IFileStorage storage,
    ILogger<ReplaceProjectImageFileCommandHandler> logger)
    : IRequestHandler<ReplaceProjectImageFileCommand, Result<ProjectImageDto>>
{
    /// <inheritdoc />
    public async Task<Result<ProjectImageDto>> Handle(ReplaceProjectImageFileCommand command, CancellationToken ct)
    {
        // 1. The project and the picture
        var project = await projects.GetByIdAsync(command.ProjectId, ct);
        if (project is null) return ProjectErrors.NotFound;
        if (project.Images.All(i => i.Id != command.ImageId)) return ProjectErrors.ImageNotFound;

        // 2. The new file: resized, WebP, metadata stripped
        var processed = await images.ProcessAsync(command.Content, ct);
        if (processed.IsFailure) return processed.Error;

        ImageFile file;
        await using (var image = processed.Value)
        {
            file = await storage.SaveImageAsync(image, $"projects/{project.Id:N}", Guid.CreateVersion7(), ct);
        }

        // 3. Swapped on the project, then saved
        ImageFile previous;
        try
        {
            var replaced = project.ReplaceImageFile(command.ImageId, file);
            if (replaced.IsFailure)
            {
                await DeleteQuietlyAsync(file);
                return replaced.Error;
            }

            previous = replaced.Value;
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            await DeleteQuietlyAsync(file);
            throw;
        }

        // 4. The old files are no longer used
        await DeleteQuietlyAsync(previous);
        return ProjectImageDto.From(project.Images.First(i => i.Id == command.ImageId), storage);
    }

    /// <summary>Removes files that won't be used; a failure here is only logged.</summary>
    private async Task DeleteQuietlyAsync(ImageFile file)
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
