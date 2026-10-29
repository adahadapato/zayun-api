using Zayun.Domain.Entities;
using Zayun.Domain.Enums;

namespace Zayun.Persistence.Seeding;

/// <summary>The shape of <c>starter-data.json</c>: today's website content, used to fill an empty database.</summary>
/// <remarks>Lists keep their order: the first entry becomes position 0.</remarks>
internal sealed class StarterData
{
    public List<StarterProject> Projects { get; set; } = [];
    public List<StarterPublication> Publications { get; set; } = [];
    public List<StarterPresentation> Presentations { get; set; } = [];
    public List<StarterTextGroup> ExperienceAreas { get; set; } = [];
    public List<StarterTextGroup> SkillGroups { get; set; } = [];
    public List<string> CoreExpertise { get; set; } = [];
    public List<StarterAward> Awards { get; set; } = [];
    public List<StarterProfile> Profiles { get; set; } = [];
    public StarterPersonalDetails? PersonalDetails { get; set; }
}

/// <summary>The owner's personal details (only what the website already showed).</summary>
internal sealed class StarterPersonalDetails
{
    public string FullName { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? PublicEmail { get; set; }
    public List<PhoneInput> Phones { get; set; } = [];
}

/// <summary>One online profile.</summary>
internal sealed class StarterProfile
{
    public ProfilePlatform Platform { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool ShowInFooter { get; set; }
    public bool ShowOnContact { get; set; }
    public bool ShowOnResearch { get; set; }
}

/// <summary>One award; <see cref="Image"/> (optional) is relative to the images folder.</summary>
internal sealed class StarterAward
{
    public string Title { get; set; } = string.Empty;
    public string AwardedBy { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Url { get; set; }
    public bool IsFeatured { get; set; }
    public string? Image { get; set; }
    public string? ImageAlt { get; set; }
}

/// <summary>One project, with its pictures.</summary>
internal sealed class StarterProject
{
    public string Title { get; set; } = string.Empty;
    /// <summary>Optional; made from the title when empty.</summary>
    public string? Slug { get; set; }
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectCategory Category { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? VisualLabel { get; set; }
    public string? GithubUrl { get; set; }
    public string? LiveUrl { get; set; }
    public bool IsPrivateRepo { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<StarterProjectImage> Images { get; set; } = [];
}

/// <summary>One project picture; <see cref="File"/> is relative to the images folder.</summary>
internal sealed class StarterProjectImage
{
    public string File { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public string? Label { get; set; }
    public ProjectImageKind Kind { get; set; }
}

/// <summary>One publication.</summary>
internal sealed class StarterPublication
{
    public string Title { get; set; } = string.Empty;
    public string Authors { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public int Year { get; set; }
    public PublicationType Type { get; set; }
    public string? Award { get; set; }
    public List<StarterLink> Links { get; set; } = [];
}

/// <summary>One publication link.</summary>
internal sealed class StarterLink
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

/// <summary>One presentation; <see cref="Image"/> is relative to the images folder.</summary>
internal sealed class StarterPresentation
{
    public string Title { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public int Year { get; set; }
    public PresentationType Type { get; set; }
    public string? Image { get; set; }
    public string? ImageAlt { get; set; }
}

/// <summary>An experience area or a skill group: a title, a description and a list of skills.</summary>
internal sealed class StarterTextGroup
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = [];
}
