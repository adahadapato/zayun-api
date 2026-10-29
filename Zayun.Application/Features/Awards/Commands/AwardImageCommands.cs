using MediatR;
using Microsoft.Extensions.Logging;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Awards.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.ValueObjects;

namespace Zayun.Application.Features.Awards.Commands;

/// <summary>Admin → Edit award → Picture: sets or replaces the certificate or photo.</summary>
/// <param name="Id">The award.</param>
/// <param name="Content">The uploaded file.</param>
/// <param name="SizeBytes">Its size, as sent.</param>
/// <param name="AltText">Text for screen readers.</param>
[InvalidatesCache(CacheTags.Awards)]
public sealed record SetAwardImageCommand(Guid Id, Stream Content, long SizeBytes, string AltText)
    : IRequest<Result<AwardDto>>;

/// <summary>Handles <see cref="SetAwardImageCommand"/>.</summary>
/// <remarks>The new files are saved first; the old ones are deleted only after the change is saved.</remarks>
public sealed class SetAwardImageCommandHandler(
    IAwardRepository awards,
    IUnitOfWork unitOfWork,
    IImageProcessor images,
    IFileStorage storage,
    ILogger<SetAwardImageCommandHandler> logger)
    : IRequestHandler<SetAwardImageCommand, Result<AwardDto>>
{
    /// <inheritdoc />
    public async Task<Result<AwardDto>> Handle(SetAwardImageCommand c, CancellationToken ct)
    {
        var award = await awards.GetByIdAsync(c.Id, ct);
        if (award is null) return AwardErrors.NotFound;

        var processed = await images.ProcessAsync(c.Content, ct);
        if (processed.IsFailure) return processed.Error;

        ImageFile file;
        await using (var image = processed.Value)
        {
            file = await storage.SaveImageAsync(image, $"awards/{award.Id:N}", Guid.CreateVersion7(), ct);
        }

        ImageFile? previous;
        try
        {
            previous = award.SetImage(file, c.AltText);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            await AwardFiles.DeleteQuietlyAsync(storage, file, logger);
            throw;
        }

        if (previous is not null) await AwardFiles.DeleteQuietlyAsync(storage, previous, logger);
        return AwardDto.From(award, storage);
    }
}

/// <summary>Admin → Edit award: removes the picture (and its files).</summary>
/// <param name="Id">The award.</param>
[InvalidatesCache(CacheTags.Awards)]
public sealed record RemoveAwardImageCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="RemoveAwardImageCommand"/>.</summary>
public sealed class RemoveAwardImageCommandHandler(
    IAwardRepository awards,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    ILogger<RemoveAwardImageCommandHandler> logger)
    : IRequestHandler<RemoveAwardImageCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RemoveAwardImageCommand c, CancellationToken ct)
    {
        var award = await awards.GetByIdAsync(c.Id, ct);
        if (award is null) return AwardErrors.NotFound;

        var removed = award.RemoveImage();
        if (removed.IsFailure) return removed.Error;

        await unitOfWork.SaveChangesAsync(ct);
        await AwardFiles.DeleteQuietlyAsync(storage, removed.Value, logger);
        return Result.Success();
    }
}

/// <summary>Deleting picture files that are no longer used; a failure is only logged.</summary>
internal static class AwardFiles
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
