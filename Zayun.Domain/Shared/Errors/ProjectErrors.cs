using Zayun.Domain.Enums;

namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for projects and their pictures. Nothing here is thrown.</summary>
public static class ProjectErrors
{
    /// <summary>No project with this Id or address (or it isn't published).</summary>
    public static readonly Error NotFound = Error.NotFound(
        "Project.NotFound", "That project wasn't found.");

    /// <summary>Another project already uses this web address.</summary>
    public static readonly Error SlugTaken = Error.Conflict(
        "Project.SlugTaken", "Another project already uses this web address. Please choose a different one.");

    /// <summary>The title has no letters or digits to make a web address from.</summary>
    public static readonly Error SlugEmpty = Error.Validation(
        "Project.SlugEmpty", "Please give the project a title or web address with at least one letter or digit.");

    /// <summary>No picture with this Id on the project.</summary>
    public static readonly Error ImageNotFound = Error.NotFound(
        "Project.ImageNotFound", "That picture isn't on this project any more.");

    /// <summary>The project already has the most pictures allowed.</summary>
    public static readonly Error TooManyImages = Error.Conflict(
        "Project.TooManyImages", "This project already has the most pictures allowed. Remove one first.");

    /// <summary>The same file is already on the project.</summary>
    public static readonly Error DuplicateImage = Error.Conflict(
        "Project.DuplicateImage", "This picture is already on the project.");

    /// <summary>Only one Primary and one Secondary picture per project.</summary>
    /// <param name="kind">The kind already in use.</param>
    /// <returns>The error.</returns>
    public static Error ImageKindTaken(ProjectImageKind kind) => Error.Conflict(
        "Project.ImageKindTaken",
        $"This project already has a {kind.ToString().ToLowerInvariant()} picture. Change or remove that one first.");
}
