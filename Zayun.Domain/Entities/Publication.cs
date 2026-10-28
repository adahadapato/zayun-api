using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// A research output listed under "Publications" on the Research page: a paper or article, where it appeared, who
/// wrote it, and links to read it.
/// </summary>
/// <remarks>Bad text is caught by the validators first, so the checks here THROW.</remarks>
public class Publication : ContentEntity
{
    /// <summary>Longest title.</summary>
    public const int TitleMaxLength = 300;

    /// <summary>Longest author list, e.g. "Enobong Adahada, Isabel Sassoon, Kate Hone and Yongmin Li".</summary>
    public const int AuthorsMaxLength = 600;

    /// <summary>Longest venue, e.g. "ICCVDM 2025" or a journal with volume and pages.</summary>
    public const int VenueMaxLength = 500;

    /// <summary>Longest award, e.g. "Best Paper Award".</summary>
    public const int AwardMaxLength = 100;

    /// <summary>Earliest year accepted.</summary>
    public const int MinYear = 1950;

    /// <summary>Latest year accepted.</summary>
    public const int MaxYear = 2100;

    /// <summary>Most links.</summary>
    public const int MaxLinks = 6;

    private readonly List<PublicationLink> _links = [];

    /// <summary>Required by EF Core.</summary>
    protected Publication() { }

    private Publication(Guid id, int displayOrder, bool isPublished) : base(id, displayOrder, isPublished) { }

    /// <summary>The paper's title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Everyone who wrote it, as printed.</summary>
    public string Authors { get; private set; } = string.Empty;

    /// <summary>Where it appeared.</summary>
    public string Venue { get; private set; } = string.Empty;

    /// <summary>The year it was published.</summary>
    public int Year { get; private set; }

    /// <summary>Conference paper, journal article…</summary>
    public PublicationType Type { get; private set; }

    /// <summary>An award it received, e.g. "Best Paper Award"; null for none.</summary>
    public string? Award { get; private set; }

    /// <summary>Where to read it (IEEE Xplore, ResearchGate…), in the order shown.</summary>
    public virtual IReadOnlyList<PublicationLink> Links => _links.AsReadOnly();

    /// <summary>Adds a publication.</summary>
    /// <param name="details">Everything about it except its place.</param>
    /// <param name="displayOrder">Its place in the list.</param>
    /// <param name="isPublished">Shown straight away, or kept as a draft.</param>
    /// <returns>The new publication.</returns>
    /// <exception cref="ArgumentException">Missing text, a bad link or an unknown type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, a year out of range, or too many links.</exception>
    public static Publication Create(PublicationDetails details, int displayOrder, bool isPublished)
    {
        var publication = new Publication(Guid.CreateVersion7(), displayOrder, isPublished);
        publication.Update(details);
        return publication;
    }

    /// <summary>Changes everything about the publication except its place.</summary>
    /// <param name="details">The new details.</param>
    /// <exception cref="ArgumentException">Missing text, a bad link or an unknown type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, a year out of range, or too many links.</exception>
    public void Update(PublicationDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        if (!Enum.IsDefined(details.Type))
            throw new ArgumentException("Unknown publication type.", nameof(details));
        ArgumentOutOfRangeException.ThrowIfLessThan(details.Year, MinYear, nameof(details.Year));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(details.Year, MaxYear, nameof(details.Year));

        var links = (details.Links ?? [])
            .Select(l => PublicationLink.Create(l.Label, l.Url, l.IsPrimary))
            .ToList();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(links.Count, MaxLinks, nameof(details.Links));

        Title = TextRules.Required(details.Title, TitleMaxLength, nameof(details.Title));
        Authors = TextRules.Required(details.Authors, AuthorsMaxLength, nameof(details.Authors));
        Venue = TextRules.Required(details.Venue, VenueMaxLength, nameof(details.Venue));
        Year = details.Year;
        Type = details.Type;
        Award = TextRules.Optional(details.Award, AwardMaxLength, nameof(details.Award));

        _links.Clear();
        _links.AddRange(links);
    }
}

/// <summary>Everything about a publication an admin types in.</summary>
/// <param name="Title">The paper's title.</param>
/// <param name="Authors">Everyone who wrote it.</param>
/// <param name="Venue">Where it appeared.</param>
/// <param name="Year">The year.</param>
/// <param name="Type">Conference paper, journal article…</param>
/// <param name="Award">Optional award.</param>
/// <param name="Links">Where to read it.</param>
public sealed record PublicationDetails(
    string Title,
    string Authors,
    string Venue,
    int Year,
    PublicationType Type,
    string? Award,
    IReadOnlyList<PublicationLinkDetails> Links);

/// <summary>One link as typed in.</summary>
/// <param name="Label">Button text, e.g. "View on IEEE Xplore".</param>
/// <param name="Url">The address.</param>
/// <param name="IsPrimary">Shown as the main button.</param>
public sealed record PublicationLinkDetails(string Label, string Url, bool IsPrimary);
