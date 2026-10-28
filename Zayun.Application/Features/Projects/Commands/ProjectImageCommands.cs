using MediatR;
using Microsoft.Extensions.Logging;
using Zayun.Application.Common.Caching;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Projects.Commands;

/// <summary>Admin → Edit project: change a picture's alt text, caption and kind.</summary>
/// <param name="ProjectId">The project.</param>
/// <param name="ImageId">The picture.</param>
/// <param name="AltText">Text for screen readers.</param>
/// <param name="Label">Optional caption.</param>
/// <param name="Kind">Primary, Secondary or Gallery.</param>
[InvalidatesCache(CacheTags.Projects)]
public sealed record UpdateProjectImageCommand(Guid ProjectId, Guid ImageId, string AltText, string? Label,
                                               ProjectImageKind Kind) : IRequest<Result>;

/// <summary>Handles <see cref="UpdateProjectImageCommand"/>.</summary>
public sealed class UpdateProjectImageCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProjectImageCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateProjectImageCommand command, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(command.ProjectId, ct);
        if (project is null) return ProjectErrors.NotFound;

        var result = project.UpdateImage(command.ImageId, command.AltText, command.Label, command.Kind);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

/// <summary>Admin → Edit project: remove a picture (the row and its files).</summary>
/// <param name="ProjectId">The project.</param>
/// <param name="ImageId">The picture.</param>
[InvalidatesCache(CacheTags.Projects)]
public sealed record DeleteProjectImageCommand(Guid ProjectId, Guid ImageId) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteProjectImageCommand"/>.</summary>
/// <remarks>The row goes first; the files after. A file that can't be deleted is only logged: the picture is
/// already gone from the site.</remarks>
public sealed class DeleteProjectImageCommandHandler(
    IProjectRepository projects,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    ILogger<DeleteProjectImageCommandHandler> logger)
    : IRequestHandler<DeleteProjectImageCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeleteProjectImageCommand command, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(command.ProjectId, ct);
        if (project is null) return ProjectErrors.NotFound;

        var removed = project.RemoveImage(command.ImageId);
        if (removed.IsFailure) return removed.Error;

        await unitOfWork.SaveChangesAsync(ct);

        try
        {
            await storage.DeleteImageAsync(removed.Value.File, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Picture {ImageId} was removed, but its files couldn't be deleted.", command.ImageId);
        }

        return Result.Success();
    }
}

/// <summary>Admin → Edit project: drag-to-reorder the pictures. Send every picture Id in the new order.</summary>
/// <param name="ProjectId">The project.</param>
/// <param name="ImageIds">Every picture Id, each once.</param>
[InvalidatesCache(CacheTags.Projects)]
public sealed record ReorderProjectImagesCommand(Guid ProjectId, IReadOnlyList<Guid> ImageIds) : IRequest<Result>;

/// <summary>Handles <see cref="ReorderProjectImagesCommand"/>.</summary>
public sealed class ReorderProjectImagesCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork)
    : IRequestHandler<ReorderProjectImagesCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReorderProjectImagesCommand command, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(command.ProjectId, ct);
        if (project is null) return ProjectErrors.NotFound;

        var result = project.ReorderImages(command.ImageIds);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
