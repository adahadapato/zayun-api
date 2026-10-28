using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.Shared.Text;
using Zayun.Domain.ValueObjects;

namespace Zayun.Domain.Entities;

/// <summary>
/// Research presented at a conference or symposium, shown under "Presentations" on the Research page, usually with a
/// picture of the poster, slide or certificate.
/// </summary>
/// <remarks>Bad text is caught by the validators first, so the checks here THROW.</remarks>
public class Presentation : ContentEntity
{
    /// <summary>Longest title.</summary>
    public const int TitleMaxLength = 300;

    /// <summary>Longest event description, e.g. "Brunel Bioinformatics Symposium 2025."</summary>
    public const int EventMaxLength = 300;

    /// <summary>Longest alt text for the picture.</summary>
    public const int ImageAltMaxLength = 300;

    /// <summary>Required by EF Core.</summary>
    protected Presentation() { }

    private Presentation(Guid id, int displayOrder, bool isPublished) : base(id, displayOrder, isPublished) { }

    /// <summary>What was presented.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Where and when, as shown on the card.</summary>
    public string EventName { get; private set; } = string.Empty;

    /// <summary>The year it was presented.</summary>
    public int Year { get; private set; }

    /// <summary>Poster, oral, conference…</summary>
    public PresentationType Type { get; private set; }

    /// <summary>The poster, slide or certificate; null for none.</summary>
    public ImageFile? Image { get; private set; }

    /// <summary>Alt text for <see cref="Image"/>; null when there's no picture.</summary>
    public string? ImageAlt { get; private set; }

    /// <summary>Adds a presentation (without a picture; add one with <see cref="SetImage"/>).</summary>
    /// <param name="details">Everything about it except its place and picture.</param>
    /// <param name="displayOrder">Its place in the list.</param>
    /// <param name="isPublished">Shown straight away, or kept as a draft.</param>
    /// <returns>The new presentation.</returns>
    /// <exception cref="ArgumentException">Missing text or an unknown type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long or a year out of range.</exception>
    public static Presentation Create(PresentationDetails details, int displayOrder, bool isPublished)
    {
        var presentation = new Presentation(Guid.CreateVersion7(), displayOrder, isPublished);
        presentation.Update(details);
        return presentation;
    }

    /// <summary>Changes everything except its place and picture.</summary>
    /// <param name="details">The new details.</param>
    /// <exception cref="ArgumentException">Missing text or an unknown type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long or a year out of range.</exception>
    public void Update(PresentationDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        if (!Enum.IsDefined(details.Type))
            throw new ArgumentException("Unknown presentation type.", nameof(details));
        ArgumentOutOfRangeException.ThrowIfLessThan(details.Year, Publication.MinYear, nameof(details.Year));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(details.Year, Publication.MaxYear, nameof(details.Year));

        Title = TextRules.Required(details.Title, TitleMaxLength, nameof(details.Title));
        EventName = TextRules.Required(details.EventName, EventMaxLength, nameof(details.EventName));
        Year = details.Year;
        Type = details.Type;

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
        if (Image is null) return PresentationErrors.NoImage;

        var removed = Image;
        Image = null;
        ImageAlt = null;
        return removed;
    }
}

/// <summary>Everything about a presentation an admin types in.</summary>
/// <param name="Title">What was presented.</param>
/// <param name="EventName">Where and when.</param>
/// <param name="Year">The year.</param>
/// <param name="Type">Poster, oral, conference…</param>
/// <param name="ImageAlt">New alt text for the existing picture; null leaves it as it is.</param>
public sealed record PresentationDetails(
    string Title,
    string EventName,
    int Year,
    PresentationType Type,
    string? ImageAlt);
