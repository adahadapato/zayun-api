using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.Shared.Text;
using Zayun.Domain.ValueObjects;

namespace Zayun.Domain.Entities;

/// <summary>
/// An award or recognition, listed on the Awards page. Featured awards are also shown as highlights on the About and
/// Experience pages. It can have a picture (a certificate or a photo).
/// </summary>
/// <remarks>Bad text is caught by the validators first, so the checks here THROW.</remarks>
public class Award : ContentEntity
{
    /// <summary>Longest title, e.g. "Best Paper Award".</summary>
    public const int TitleMaxLength = 150;

    /// <summary>Longest "awarded by", e.g. "ICCVDM 2025" or "National Examinations Council (NECO)".</summary>
    public const int AwardedByMaxLength = 200;

    /// <summary>Longest description.</summary>
    public const int DescriptionMaxLength = 1000;

    /// <summary>Longest link.</summary>
    public const int UrlMaxLength = 500;

    /// <summary>Longest alt text for the picture.</summary>
    public const int ImageAltMaxLength = 300;

    /// <summary>Required by EF Core.</summary>
    protected Award() { }

    private Award(Guid id, int displayOrder, bool isPublished) : base(id, displayOrder, isPublished) { }

    /// <summary>What the award is called.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Who gave it, or the event.</summary>
    public string AwardedBy { get; private set; } = string.Empty;

    /// <summary>The year it was received.</summary>
    public int Year { get; private set; }

    /// <summary>What it was for.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Where to read more (an announcement, the paper); null for none.</summary>
    public string? Url { get; private set; }

    /// <summary>Also shown as a highlight on the About and Experience pages.</summary>
    public bool IsFeatured { get; private set; }

    /// <summary>A certificate or photo; null for none.</summary>
    public ImageFile? Image { get; private set; }

    /// <summary>Alt text for <see cref="Image"/>; null when there's no picture.</summary>
    public string? ImageAlt { get; private set; }

    /// <summary>Adds an award (without a picture; add one with <see cref="SetImage"/>).</summary>
    /// <param name="details">Everything about it except its place and picture.</param>
    /// <param name="displayOrder">Its place in the list.</param>
    /// <param name="isPublished">Shown straight away, or kept as a draft.</param>
    /// <returns>The new award.</returns>
    /// <exception cref="ArgumentException">Missing text or a bad link.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long or a year out of range.</exception>
    public static Award Create(AwardDetails details, int displayOrder, bool isPublished)
    {
        var award = new Award(Guid.CreateVersion7(), displayOrder, isPublished);
        award.Update(details);
        return award;
    }

    /// <summary>Changes everything except its place and picture.</summary>
    /// <param name="details">The new details.</param>
    /// <exception cref="ArgumentException">Missing text or a bad link.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long or a year out of range.</exception>
    public void Update(AwardDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentOutOfRangeException.ThrowIfLessThan(details.Year, Publication.MinYear, nameof(details.Year));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(details.Year, Publication.MaxYear, nameof(details.Year));

        Title = TextRules.Required(details.Title, TitleMaxLength, nameof(details.Title));
        AwardedBy = TextRules.Required(details.AwardedBy, AwardedByMaxLength, nameof(details.AwardedBy));
        Year = details.Year;
        Description = TextRules.Required(details.Description, DescriptionMaxLength, nameof(details.Description));
        Url = TextRules.OptionalUrl(details.Url, UrlMaxLength, nameof(details.Url));
        IsFeatured = details.IsFeatured;

        if (Image is not null && details.ImageAlt is not null)
            ImageAlt = TextRules.Required(details.ImageAlt, ImageAltMaxLength, nameof(details.ImageAlt));
    }

    /// <summary>Sets or replaces the picture.</summary>
    /// <param name="file">The stored image.</param>
    /// <param name="altText">Text for screen readers.</param>
    /// <returns>The picture it replaced (so its files can be deleted), or null.</returns>
    /// <exception cref="ArgumentException">Empty alt text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Alt text too long.</exception>
    public ImageFile? SetImage(ImageFile file, string altText)
    {
        ArgumentNullException.ThrowIfNull(file);
        var alt = TextRules.Required(altText, ImageAltMaxLength, nameof(altText));

        var previous = Image;
        Image = file;
        ImageAlt = alt;
        return previous;
    }

    /// <summary>Takes the picture off.</summary>
    /// <returns>The removed picture (so its files can be deleted); or NoImage.</returns>
    public Result<ImageFile> RemoveImage()
    {
        if (Image is null) return AwardErrors.NoImage;

        var removed = Image;
        Image = null;
        ImageAlt = null;
        return removed;
    }
}

/// <summary>Everything about an award an admin types in.</summary>
/// <param name="Title">What it's called.</param>
/// <param name="AwardedBy">Who gave it, or the event.</param>
/// <param name="Year">The year.</param>
/// <param name="Description">What it was for.</param>
/// <param name="Url">Where to read more; null or empty for none.</param>
/// <param name="IsFeatured">Also show it on the About and Experience pages.</param>
/// <param name="ImageAlt">New alt text for the existing picture; null leaves it as it is.</param>
public sealed record AwardDetails(
    string Title,
    string AwardedBy,
    int Year,
    string Description,
    string? Url,
    bool IsFeatured,
    string? ImageAlt);
