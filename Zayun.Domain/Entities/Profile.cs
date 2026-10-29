using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// An online profile (ORCID, ResearchGate, Google Scholar, arXiv, LinkedIn, GitHub…), managed in Admin → Profiles and
/// shown on the website where the admin chooses: the footer, the Contact page and / or the Research page. All of
/// them are also listed for search engines, so they know the profiles belong to the same person.
/// </summary>
/// <remarks>Bad text is caught by the validators first, so the checks here THROW.</remarks>
public class Profile : ContentEntity
{
    /// <summary>Longest name, e.g. "Google Scholar".</summary>
    public const int NameMaxLength = 60;

    /// <summary>Longest label, e.g. an ORCID iD "0000-0002-1825-0097".</summary>
    public const int LabelMaxLength = 100;

    /// <summary>Longest link.</summary>
    public const int UrlMaxLength = 500;

    /// <summary>Required by EF Core.</summary>
    protected Profile() { }

    private Profile(Guid id, int displayOrder, bool isPublished) : base(id, displayOrder, isPublished) { }

    /// <summary>Which site (picks the icon).</summary>
    public ProfilePlatform Platform { get; private set; }

    /// <summary>The name shown, e.g. "ORCID".</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The profile's address.</summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>A short line under the name, e.g. the ORCID iD or "@adahadapato"; null for none.</summary>
    public string? Label { get; private set; }

    /// <summary>Shown as an icon in the footer of every page.</summary>
    public bool ShowInFooter { get; private set; }

    /// <summary>Shown in "Professional profiles" on the Contact page.</summary>
    public bool ShowOnContact { get; private set; }

    /// <summary>Shown in "Find my research" on the Research page.</summary>
    public bool ShowOnResearch { get; private set; }

    /// <summary>Adds a profile.</summary>
    /// <param name="details">Everything about it except its place.</param>
    /// <param name="displayOrder">Its place in the list.</param>
    /// <param name="isPublished">Shown straight away, or kept as a draft.</param>
    /// <returns>The new profile.</returns>
    /// <exception cref="ArgumentException">Missing text, a bad link or an unknown platform.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long.</exception>
    public static Profile Create(ProfileDetails details, int displayOrder, bool isPublished)
    {
        var profile = new Profile(Guid.CreateVersion7(), displayOrder, isPublished);
        profile.Update(details);
        return profile;
    }

    /// <summary>Changes everything except its place.</summary>
    /// <param name="details">The new details.</param>
    /// <exception cref="ArgumentException">Missing text, a bad link or an unknown platform.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long.</exception>
    public void Update(ProfileDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        if (!Enum.IsDefined(details.Platform))
            throw new ArgumentException("Unknown profile platform.", nameof(details));

        Platform = details.Platform;
        Name = TextRules.Required(details.Name, NameMaxLength, nameof(details.Name));
        Url = TextRules.RequiredUrl(details.Url, UrlMaxLength, nameof(details.Url));
        Label = TextRules.Optional(details.Label, LabelMaxLength, nameof(details.Label));
        ShowInFooter = details.ShowInFooter;
        ShowOnContact = details.ShowOnContact;
        ShowOnResearch = details.ShowOnResearch;
    }
}

/// <summary>Everything about a profile an admin types in.</summary>
/// <param name="Platform">Which site (picks the icon).</param>
/// <param name="Name">The name shown.</param>
/// <param name="Url">The profile's address.</param>
/// <param name="Label">An optional short line, e.g. the ORCID iD.</param>
/// <param name="ShowInFooter">Show in the footer.</param>
/// <param name="ShowOnContact">Show on the Contact page.</param>
/// <param name="ShowOnResearch">Show on the Research page.</param>
public sealed record ProfileDetails(
    ProfilePlatform Platform,
    string Name,
    string Url,
    string? Label,
    bool ShowInFooter,
    bool ShowOnContact,
    bool ShowOnResearch);
