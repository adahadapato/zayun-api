namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for skill groups and core expertise. Nothing here is thrown.</summary>
public static class SkillErrors
{
    /// <summary>No skill group with this Id.</summary>
    public static readonly Error GroupNotFound = Error.NotFound(
        "Skill.GroupNotFound", "That skill group wasn't found.");

    /// <summary>No core expertise item with this Id.</summary>
    public static readonly Error ExpertiseNotFound = Error.NotFound(
        "Skill.ExpertiseNotFound", "That core expertise item wasn't found.");

    /// <summary>The same core expertise is already listed.</summary>
    public static readonly Error ExpertiseExists = Error.Conflict(
        "Skill.ExpertiseExists", "This core expertise is already listed.");
}
