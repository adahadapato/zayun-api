namespace Zayun.Domain.Shared.Errors;

/// <summary>Picture problems the admin can fix themselves, returned by IImageProcessor.</summary>
/// <remarks>All are Validation errors (HTTP 400): the admin area shows the message and lets them pick another file.</remarks>
public static class ImageErrors
{
    /// <summary>No file was sent, or it was empty.</summary>
    public static readonly Error Empty = Error.Validation(
        "Image.Empty", "Please choose a picture to upload.");

    /// <summary>The file isn't a readable picture.</summary>
    public static readonly Error NotAnImage = Error.Validation(
        "Image.NotAnImage", "That file isn't a picture we can read. Please use a JPEG, PNG, WebP or HEIC image.");

    /// <summary>The file is bigger than the upload limit.</summary>
    /// <param name="maxBytes">The limit in bytes.</param>
    /// <returns>The error, with the limit in MB.</returns>
    public static Error TooLarge(long maxBytes) => Error.Validation(
        "Image.TooLarge", $"This picture is too big. The maximum is {maxBytes / (1024 * 1024)} MB.");
}
