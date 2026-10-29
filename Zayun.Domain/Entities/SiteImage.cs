using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Text;
using Zayun.Domain.ValueObjects;

namespace Zayun.Domain.Entities;

/// <summary>
/// The picture in one named place on the website (see <see cref="SiteImageSlot"/>), e.g. the owner's photo on the
/// About page. One row per slot; removing the picture deletes the row.
/// </summary>
/// <remarks>Bad text is caught by the validators first, so the checks here THROW.</remarks>
public class SiteImage : Entity<Guid>
{
    /// <summary>Longest alt text.</summary>
    public const int AltTextMaxLength = 300;

    /// <summary>Required by EF Core.</summary>
    protected SiteImage() { }

    private SiteImage(Guid id) : base(id) { }

    /// <summary>Where on the website it's shown (unique).</summary>
    public SiteImageSlot Slot { get; private set; }

    /// <summary>The stored picture.</summary>
    public ImageFile Image { get; private set; } = null!;

    /// <summary>Text for screen readers.</summary>
    public string AltText { get; private set; } = string.Empty;

    /// <summary>A picture for a slot.</summary>
    /// <param name="slot">Where it's shown.</param>
    /// <param name="image">The stored picture.</param>
    /// <param name="altText">Text for screen readers.</param>
    /// <returns>The new row.</returns>
    /// <exception cref="ArgumentException">An unknown slot or empty alt text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Alt text too long.</exception>
    public static SiteImage Create(SiteImageSlot slot, ImageFile image, string altText)
    {
        if (!Enum.IsDefined(slot)) throw new ArgumentException("Unknown picture slot.", nameof(slot));
        var siteImage = new SiteImage(Guid.CreateVersion7()) { Slot = slot };
        siteImage.Replace(image, altText);
        return siteImage;
    }

    /// <summary>Puts a new picture in the slot.</summary>
    /// <param name="image">The stored picture.</param>
    /// <param name="altText">Text for screen readers.</param>
    /// <returns>The picture it replaced (so its files can be deleted).</returns>
    /// <exception cref="ArgumentException">Empty alt text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Alt text too long.</exception>
    public ImageFile? Replace(ImageFile image, string altText)
    {
        ArgumentNullException.ThrowIfNull(image);
        var alt = TextRules.Required(altText, AltTextMaxLength, nameof(altText));
        var previous = Image;
        Image = image;
        AltText = alt;
        return previous;
    }

    /// <summary>Changes the alt text only.</summary>
    /// <param name="altText">Text for screen readers.</param>
    /// <exception cref="ArgumentException">Empty alt text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Alt text too long.</exception>
    public void Describe(string altText) => AltText = TextRules.Required(altText, AltTextMaxLength, nameof(altText));
}
