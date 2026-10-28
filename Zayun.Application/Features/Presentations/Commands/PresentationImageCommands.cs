using MediatR;
using Microsoft.Extensions.Logging;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Presentations.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.ValueObjects;

namespace Zayun.Application.Features.Presentations.Commands;

/// <summary>Admin → Edit presentation → Picture: sets or replaces the poster, slide or certificate.</summary>
/// <param name="Id">The presentation.</param>
/// <param name="Content">The uploaded file.</param>
/// <param name="SizeBytes">Its size, as sent.</param>
/// <param name="AltText">Text for screen readers.</param>
[InvalidatesCache(CacheTags.Presentations)]
public sealed record SetPresentationImageCommand(Guid Id, Stream Content, long SizeBytes, string AltText)
    : IRequest<Result<PresentationDto>>;

/// <summary>Handles <see cref="SetPresentationImageCommand"/>.</summary>
/// <remarks>The new files are saved first; the old ones are deleted only after the change is saved.</remarks>
public sealed class SetPresentationImageCommandHandler(
    IPresentationRepository presentations,
    IUnitOfWork unitOfWork,
    IImageProcessor images,
    IFileStorage storage,
    ILogger<SetPresentationImageCommandHandler> logger)
    : IRequestHandler<SetPresentationImageCommand, Result<PresentationDto>>
{
    /// <inheritdoc />
    public async Task<Result<PresentationDto>> Handle(SetPresentationImageCommand c, CancellationToken ct)
    {
        var presentation = await presentations.GetByIdAsync(c.Id, ct);
        if (presentation is null) return PresentationErrors.NotFound;

        var processed = await images.ProcessAsync(c.Content, ct);
        if (processed.IsFailure) return processed.Error;

        ImageFile file;
        await using (var image = processed.Value)
        {
            file = await storage.SaveImageAsync(image, $"presentations/{presentation.Id:N}", Guid.CreateVersion7(), ct);
        }

        ImageFile? previous;
        try
        {
            previous = presentation.SetImage(file, c.AltText);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            await PresentationFiles.DeleteQuietlyAsync(storage, file, logger);
            throw;
        }

        if (previous is not null) await PresentationFiles.DeleteQuietlyAsync(storage, previous, logger);
        return PresentationDto.From(presentation, storage);
    }
}

/// <summary>Admin → Edit presentation: removes the picture (and its files).</summary>
/// <param name="Id">The presentation.</param>
[InvalidatesCache(CacheTags.Presentations)]
public sealed record RemovePresentationImageCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="RemovePresentationImageCommand"/>.</summary>
public sealed class RemovePresentationImageCommandHandler(
    IPresentationRepository presentations,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    ILogger<RemovePresentationImageCommandHandler> logger)
    : IRequestHandler<RemovePresentationImageCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RemovePresentationImageCommand c, CancellationToken ct)
    {
        var presentation = await presentations.GetByIdAsync(c.Id, ct);
        if (presentation is null) return PresentationErrors.NotFound;

        var removed = presentation.RemoveImage();
        if (removed.IsFailure) return removed.Error;

        await unitOfWork.SaveChangesAsync(ct);
        await PresentationFiles.DeleteQuietlyAsync(storage, removed.Value, logger);
        return Result.Success();
    }
}

/// <summary>Deleting picture files that are no longer used; a failure is only logged.</summary>
internal static class PresentationFiles
{
    public static async Task DeleteQuietlyAsync(IFileStorage storage, ImageFile file, ILogger logger)
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
