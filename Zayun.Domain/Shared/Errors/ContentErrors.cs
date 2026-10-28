namespace Zayun.Domain.Shared.Errors;

/// <summary>Failures shared by every kind of website content. Nothing here is thrown.</summary>
public static class ContentErrors
{
    /// <summary>A reorder request that doesn't list every item of the list exactly once.</summary>
    public static readonly Error ReorderMismatch = Error.Validation(
        "Content.ReorderMismatch",
        "The new order must list every item exactly once. Refresh the page and try again.");
}
