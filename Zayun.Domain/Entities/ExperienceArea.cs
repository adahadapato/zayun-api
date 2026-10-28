using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// One area of professional experience on the Experience page, e.g. "Technical Leadership", with what it involved and
/// the skills used.
/// </summary>
/// <remarks>Bad text is caught by the validators first, so the checks here THROW.</remarks>
public class ExperienceArea : ContentEntity
{
    /// <summary>Longest title.</summary>
    public const int TitleMaxLength = 150;

    /// <summary>Longest description.</summary>
    public const int DescriptionMaxLength = 2000;

    /// <summary>Longest skill.</summary>
    public const int SkillMaxLength = 60;

    /// <summary>Most skills.</summary>
    public const int MaxSkills = 30;

    /// <summary>Required by EF Core.</summary>
    protected ExperienceArea() { }

    private ExperienceArea(Guid id, int displayOrder, bool isPublished) : base(id, displayOrder, isPublished) { }

    /// <summary>The area's name.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>What the experience involved.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Skills used, in the order shown.</summary>
    public List<string> Skills { get; private set; } = [];

    /// <summary>Adds an experience area.</summary>
    /// <param name="title">The area's name.</param>
    /// <param name="description">What it involved.</param>
    /// <param name="skills">Skills used.</param>
    /// <param name="displayOrder">Its place in the list.</param>
    /// <param name="isPublished">Shown straight away, or kept as a draft.</param>
    /// <returns>The new area.</returns>
    /// <exception cref="ArgumentException">Missing text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or too many skills.</exception>
    public static ExperienceArea Create(string title, string description, IEnumerable<string> skills,
                                        int displayOrder, bool isPublished)
    {
        var area = new ExperienceArea(Guid.CreateVersion7(), displayOrder, isPublished);
        area.Update(title, description, skills);
        return area;
    }

    /// <summary>Changes the text and skills.</summary>
    /// <param name="title">The area's name.</param>
    /// <param name="description">What it involved.</param>
    /// <param name="skills">Skills used.</param>
    /// <exception cref="ArgumentException">Missing text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or too many skills.</exception>
    public void Update(string title, string description, IEnumerable<string> skills)
    {
        Title = TextRules.Required(title, TitleMaxLength, nameof(title));
        Description = TextRules.Required(description, DescriptionMaxLength, nameof(description));
        Skills = TextRules.Labels(skills, SkillMaxLength, MaxSkills, nameof(skills));
    }
}
