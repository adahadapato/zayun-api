using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// A group of skills on the Skills page, e.g. "Artificial Intelligence &amp; Machine Learning", with a short description
/// and the skills in it.
/// </summary>
/// <remarks>Bad text is caught by the validators first, so the checks here THROW.</remarks>
public class SkillGroup : ContentEntity
{
    /// <summary>Longest title.</summary>
    public const int TitleMaxLength = 150;

    /// <summary>Longest description.</summary>
    public const int DescriptionMaxLength = 2000;

    /// <summary>Longest skill.</summary>
    public const int SkillMaxLength = 60;

    /// <summary>Most skills.</summary>
    public const int MaxSkills = 40;

    /// <summary>Required by EF Core.</summary>
    protected SkillGroup() { }

    private SkillGroup(Guid id, int displayOrder, bool isPublished) : base(id, displayOrder, isPublished) { }

    /// <summary>The group's name.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>What the group covers.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>The skills, in the order shown.</summary>
    public List<string> Skills { get; private set; } = [];

    /// <summary>Adds a skill group.</summary>
    /// <param name="title">The group's name.</param>
    /// <param name="description">What it covers.</param>
    /// <param name="skills">The skills.</param>
    /// <param name="displayOrder">Its place in the list.</param>
    /// <param name="isPublished">Shown straight away, or kept as a draft.</param>
    /// <returns>The new group.</returns>
    /// <exception cref="ArgumentException">Missing text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or too many skills.</exception>
    public static SkillGroup Create(string title, string description, IEnumerable<string> skills,
                                        int displayOrder, bool isPublished)
    {
        var group = new SkillGroup(Guid.CreateVersion7(), displayOrder, isPublished);
        group.Update(title, description, skills);
        return group;
    }

    /// <summary>Changes the text and skills.</summary>
    /// <param name="title">The group's name.</param>
    /// <param name="description">What it covers.</param>
    /// <param name="skills">The skills.</param>
    /// <exception cref="ArgumentException">Missing text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or too many skills.</exception>
    public void Update(string title, string description, IEnumerable<string> skills)
    {
        Title = TextRules.Required(title, TitleMaxLength, nameof(title));
        Description = TextRules.Required(description, DescriptionMaxLength, nameof(description));
        Skills = TextRules.Labels(skills, SkillMaxLength, MaxSkills, nameof(skills));
    }

    /// <summary>Swaps one skill name for another (the tag was renamed or merged).</summary>
    /// <param name="oldName">The name used now.</param>
    /// <param name="newName">The name to use instead.</param>
    /// <returns>True when the old name was in the list.</returns>
    public bool ReplaceSkill(string oldName, string newName)
    {
        var replaced = TextRules.ReplaceLabel(Skills, oldName, newName);
        if (replaced is null) return false;
        Skills = replaced;
        return true;
    }
}
