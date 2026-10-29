using MediatR;
using Microsoft.Extensions.Logging;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.SiteImages.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.ValueObjects;

namespace Zayun.Application.Features.SiteImages.Commands;

/// <summary>Admin → Photo: uploads (or replaces) the picture in a slot, resized to WebP like any other.</summary>
/// <param name="Slot">Where it's shown.</param>
/// <param name="Content">The uploaded file.</param>
/// <param name="SizeBytes">Its size, as sent.</param>
/// <param name="AltText">Text for screen readers.</param>
[InvalidatesCache(CacheTags.SiteImages)]
public sealed record SetSiteImageCommand(SiteImageSlot Slot, Stream Content, long SizeBytes, string AltText)
    : IRequest<Result<SiteImageDto>>;

/// <summary>Handles <see cref="SetSiteImageCommand"/>.</summary>
/// <remarks>The new files get a new name (browsers never show the old cached picture); the old ones are deleted only
/// after the change is saved.</remarks>
public sealed class SetSiteImageCommandHandler(ISiteImageRepository images, IUnitOfWork unitOfWork,
                                               IImageProcessor processor, IFileStorage storage,
                                               ILogger<SetSiteImageCommandHandler> logger)
    : IRequestHandler<SetSiteImageCommand, Result<SiteImageDto>>
{
    /// <inheritdoc />
    public async Task<Result<SiteImageDto>> Handle(SetSiteImageCommand c, CancellationToken ct)
    {
        var processed = await processor.ProcessAsync(c.Content, ct);
        if (processed.IsFailure) return processed.Error;

        ImageFile file;
        await using (var image = processed.Value)
        {
            file = await storage.SaveImageAsync(image, $"site/{c.Slot.ToString().ToLowerInvariant()}",
                                                Guid.CreateVersion7(), ct);
        }

        ImageFile? previous = null;
        SiteImage siteImage;
        try
        {
            var existing = await images.GetBySlotAsync(c.Slot, ct);
            if (existing is null)
            {
                siteImage = SiteImage.Create(c.Slot, file, c.AltText);
                images.Add(siteImage);
            }
            else
            {
                siteImage = existing;
                previous = existing.Replace(file, c.AltText);
            }

            await unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            await SiteImageFiles.DeleteQuietlyAsync(storage, file, logger);
            throw;
        }

        if (previous is not null) await SiteImageFiles.DeleteQuietlyAsync(storage, previous, logger);
        return SiteImageDto.From(siteImage, storage);
    }
}

/// <summary>Admin → Photo: changes the picture's description only.</summary>
/// <param name="Slot">Where it's shown.</param>
/// <param name="AltText">Text for screen readers.</param>
[InvalidatesCache(CacheTags.SiteImages)]
public sealed record DescribeSiteImageCommand(SiteImageSlot Slot, string AltText) : IRequest<Result<SiteImageDto>>;

/// <summary>Handles <see cref="DescribeSiteImageCommand"/>.</summary>
public sealed class DescribeSiteImageCommandHandler(ISiteImageRepository images, IUnitOfWork unitOfWork,
                                                    IFileStorage storage)
    : IRequestHandler<DescribeSiteImageCommand, Result<SiteImageDto>>
{
    /// <inheritdoc />
    public async Task<Result<SiteImageDto>> Handle(DescribeSiteImageCommand c, CancellationToken ct)
    {
        var siteImage = await images.GetBySlotAsync(c.Slot, ct);
        if (siteImage is null) return SiteImageErrors.Empty;

        siteImage.Describe(c.AltText);
        await unitOfWork.SaveChangesAsync(ct);
        return SiteImageDto.From(siteImage, storage);
    }
}

/// <summary>Admin → Photo: removes the picture (and its files); the page then shows without one.</summary>
/// <param name="Slot">Where it's shown.</param>
[InvalidatesCache(CacheTags.SiteImages)]
public sealed record RemoveSiteImageCommand(SiteImageSlot Slot) : IRequest<Result>;

/// <summary>Handles <see cref="RemoveSiteImageCommand"/>.</summary>
public sealed class RemoveSiteImageCommandHandler(ISiteImageRepository images, IUnitOfWork unitOfWork,
                                                  IFileStorage storage, ILogger<RemoveSiteImageCommandHandler> logger)
    : IRequestHandler<RemoveSiteImageCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RemoveSiteImageCommand c, CancellationToken ct)
    {
        var siteImage = await images.GetBySlotAsync(c.Slot, ct);
        if (siteImage is null) return SiteImageErrors.Empty;

        var file = siteImage.Image;
        images.Remove(siteImage);
        await unitOfWork.SaveChangesAsync(ct);
        await SiteImageFiles.DeleteQuietlyAsync(storage, file, logger);
        return Result.Success();
    }
}

/// <summary>Deleting picture files that are no longer used; a failure is only logged.</summary>
internal static class SiteImageFiles
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
