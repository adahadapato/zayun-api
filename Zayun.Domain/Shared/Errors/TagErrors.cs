namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for tags. Nothing here is thrown.</summary>
public static class TagErrors
{
    /// <summary>No tag with this Id.</summary>
    public static readonly Error NotFound = Error.NotFound("Tag.NotFound", "That tag wasn't found.");

    /// <summary>Another tag already has this name (or means the same).</summary>
    /// <param name="existing">The tag that already has it.</param>
    public static Error AlreadyExists(string existing) => Error.Conflict(
        "Tag.AlreadyExists", $"This is already the tag \"{existing}\". Use Merge to combine the two.");

    /// <summary>A tag can't be merged into itself.</summary>
    public static readonly Error MergeIntoSelf = Error.Validation(
        "Tag.MergeIntoSelf", "Choose a different tag to merge into.");

    /// <summary>The tag is still used, so it can't be deleted.</summary>
    /// <param name="count">How many items use it.</param>
    public static Error InUse(int count) => Error.Conflict(
        "Tag.InUse", $"This tag is used by {count} {(count == 1 ? "item" : "items")}. Merge it into another tag, " +
                     "or remove it from them first.");
}
