using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Logging;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Quotes.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.ValueObjects;
using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Quotes.Commands;

/// <summary>Admin → Edit quote → Picture: sets or replaces the still picture (resized and converted to WebP, with a
/// thumbnail, like every other picture).</summary>
/// <param name="Id">The quote.</param>
/// <param name="Content">The uploaded file.</param>
/// <param name="SizeBytes">Its size, as sent.</param>
/// <param name="AltText">Its description for screen readers (it should tell the story the picture shows).</param>
[InvalidatesCache(CacheTags.Quotes)]
public sealed record SetQuotePictureCommand(Guid Id, Stream Content, long SizeBytes, string AltText)
    : IRequest<Result<QuoteDto>>;

/// <summary>Handles <see cref="SetQuotePictureCommand"/>.</summary>
/// <remarks>The new files are saved first; the old ones are deleted only after the change is saved.</remarks>
public sealed class SetQuotePictureCommandHandler(
    IQuoteRepository quotes,
    IUnitOfWork unitOfWork,
    IImageProcessor images,
    IFileStorage storage,
    ILogger<SetQuotePictureCommandHandler> logger)
    : IRequestHandler<SetQuotePictureCommand, Result<QuoteDto>>
{
    /// <inheritdoc />
    public async Task<Result<QuoteDto>> Handle(SetQuotePictureCommand c, CancellationToken ct)
    {
        var quote = await quotes.GetByIdAsync(c.Id, ct);
        if (quote is null) return QuoteErrors.NotFound;

        var processed = await images.ProcessAsync(c.Content, ct);
        if (processed.IsFailure) return processed.Error;

        ImageFile file;
        await using (var image = processed.Value)
        {
            file = await storage.SaveImageAsync(image, QuoteMediaFiles.Folder(quote.Id), Guid.CreateVersion7(), ct);
        }

        ImageFile? previous;
        try
        {
            previous = quote.SetPicture(file, c.AltText);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            await QuoteMediaFiles.DeleteQuietlyAsync(storage, file, logger);
            throw;
        }

        if (previous is not null) await QuoteMediaFiles.DeleteQuietlyAsync(storage, previous, logger);
        return QuoteDto.From(quote, storage);
    }
}

/// <summary>Admin → Edit quote: removes the picture, and the animation with it (and their files).</summary>
/// <param name="Id">The quote.</param>
[InvalidatesCache(CacheTags.Quotes)]
public sealed record RemoveQuotePictureCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="RemoveQuotePictureCommand"/>.</summary>
public sealed class RemoveQuotePictureCommandHandler(
    IQuoteRepository quotes,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    ILogger<RemoveQuotePictureCommandHandler> logger)
    : IRequestHandler<RemoveQuotePictureCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RemoveQuotePictureCommand c, CancellationToken ct)
    {
        var quote = await quotes.GetByIdAsync(c.Id, ct);
        if (quote is null) return QuoteErrors.NotFound;

        var removed = quote.RemovePicture();
        if (removed.IsFailure) return removed.Error;

        await unitOfWork.SaveChangesAsync(ct);
        await QuoteMediaFiles.DeleteQuietlyAsync(storage, removed.Value.Picture, logger);
        if (removed.Value.Animation is not null)
            await QuoteMediaFiles.DeleteQuietlyAsync(storage, removed.Value.Animation, logger);
        return Result.Success();
    }
}

/// <summary>Admin → Edit quote → Animation: sets or replaces the MP4 animation. The quote needs its still picture
/// first.</summary>
/// <param name="Id">The quote.</param>
/// <param name="Content">The uploaded file.</param>
/// <param name="SizeBytes">Its size, as sent.</param>
[InvalidatesCache(CacheTags.Quotes)]
public sealed record SetQuoteAnimationCommand(Guid Id, Stream Content, long SizeBytes) : IRequest<Result<QuoteDto>>;

/// <summary>Handles <see cref="SetQuoteAnimationCommand"/>.</summary>
/// <remarks>The file is checked to really be an MP4 (not just named .mp4), stored as it is, and the old animation is
/// deleted only after the change is saved.</remarks>
public sealed class SetQuoteAnimationCommandHandler(
    IQuoteRepository quotes,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    ILogger<SetQuoteAnimationCommandHandler> logger)
    : IRequestHandler<SetQuoteAnimationCommand, Result<QuoteDto>>
{
    /// <inheritdoc />
    public async Task<Result<QuoteDto>> Handle(SetQuoteAnimationCommand c, CancellationToken ct)
    {
        var quote = await quotes.GetByIdAsync(c.Id, ct);
        if (quote is null) return QuoteErrors.NotFound;
        if (quote.Picture is null) return QuoteErrors.AnimationNeedsPicture;   // before reading the upload

        var stored = await QuoteMediaFiles.StoreAnimationAsync(storage, c.Content, quote.Id, ct);
        if (stored.IsFailure) return stored.Error;
        var file = stored.Value;

        Result<ReplacedAnimation> replaced;
        try
        {
            replaced = quote.SetAnimation(file);
            if (replaced.IsFailure)
            {
                await QuoteMediaFiles.DeleteQuietlyAsync(storage, file, logger);
                return replaced.Error;
            }

            await unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            await QuoteMediaFiles.DeleteQuietlyAsync(storage, file, logger);
            throw;
        }

        if (replaced.Value.Previous is not null)
            await QuoteMediaFiles.DeleteQuietlyAsync(storage, replaced.Value.Previous, logger);
        return QuoteDto.From(quote, storage);
    }
}

/// <summary>Admin → Edit quote: removes the animation (and its file); the picture stays.</summary>
/// <param name="Id">The quote.</param>
[InvalidatesCache(CacheTags.Quotes)]
public sealed record RemoveQuoteAnimationCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="RemoveQuoteAnimationCommand"/>.</summary>
public sealed class RemoveQuoteAnimationCommandHandler(
    IQuoteRepository quotes,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    ILogger<RemoveQuoteAnimationCommandHandler> logger)
    : IRequestHandler<RemoveQuoteAnimationCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RemoveQuoteAnimationCommand c, CancellationToken ct)
    {
        var quote = await quotes.GetByIdAsync(c.Id, ct);
        if (quote is null) return QuoteErrors.NotFound;

        var removed = quote.RemoveAnimation();
        if (removed.IsFailure) return removed.Error;

        await unitOfWork.SaveChangesAsync(ct);
        await QuoteMediaFiles.DeleteQuietlyAsync(storage, removed.Value, logger);
        return Result.Success();
    }
}

/// <summary>Storing and deleting a quote's picture and animation files.</summary>
internal static class QuoteMediaFiles
{
    /// <summary>The quote's folder in the public uploads, e.g. <c>quotes/0193…</c>.</summary>
    /// <param name="quoteId">The quote.</param>
    public static string Folder(Guid quoteId) => $"quotes/{quoteId:N}";

    /// <summary>Checks the upload really is an MP4 within the size limit, and stores it under a new name.</summary>
    /// <param name="storage">The public file store.</param>
    /// <param name="upload">The uploaded file.</param>
    /// <param name="quoteId">The quote (for its folder).</param>
    /// <param name="ct">Cancels the upload.</param>
    /// <returns>The stored animation; or AnimationEmpty, AnimationTooLarge or AnimationNotMp4.</returns>
    public static async Task<Result<AnimationFile>> StoreAnimationAsync(IFileStorage storage, Stream upload,
                                                                         Guid quoteId, CancellationToken ct)
    {
        // 1. Read it, refusing anything over the limit without loading it all
        var bytes = await ReadLimitedAsync(upload, AnimationFile.MaxFileSizeBytes, ct);
        if (bytes is null) return QuoteErrors.AnimationTooLarge;
        if (bytes.Length == 0) return QuoteErrors.AnimationEmpty;

        // 2. What it really is: an MP4 has "ftyp" at bytes 4–7
        if (!IsMp4(bytes)) return QuoteErrors.AnimationNotMp4;

        // 3. Store it under a new name
        var path = $"{Folder(quoteId)}/{Guid.CreateVersion7():N}.mp4";
        using (var stream = new MemoryStream(bytes, writable: false))
            path = await storage.SaveAsync(stream, path, AnimationFile.Mp4, ct);

        return AnimationFile.Create(path, AnimationFile.Mp4, bytes.Length,
                                    Convert.ToHexString(SHA256.HashData(bytes)));
    }

    /// <summary>Deletes picture files that are no longer used; a failure is only logged.</summary>
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

    /// <summary>Deletes an animation file that is no longer used; a failure is only logged.</summary>
    public static async Task DeleteQuietlyAsync(IFileStorage storage, AnimationFile file, ILogger logger)
    {
        try
        {
            await storage.DeleteAsync(file.Path, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't delete unused animation file {Path}.", file.Path);
        }
    }

    /// <summary>True for an MP4 (ISO base media file): the box type "ftyp" at bytes 4–7.</summary>
    internal static bool IsMp4(byte[] bytes) =>
        bytes.Length >= 12 && bytes.AsSpan(4, 4).SequenceEqual("ftyp"u8);

    /// <summary>Reads the upload, or returns null as soon as it's over <paramref name="maxBytes"/>.</summary>
    private static async Task<byte[]?> ReadLimitedAsync(Stream upload, long maxBytes, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(upload);
        if (upload.CanSeek && upload.Length - upload.Position > maxBytes) return null;

        using var buffer = new MemoryStream();
        var chunk = new byte[81_920];
        int read;
        while ((read = await upload.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > maxBytes) return null;
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }
}
