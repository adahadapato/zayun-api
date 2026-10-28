using FluentValidation;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Common.Validation;

/// <summary>Rules used by many validators, with messages written for the person filling in the form.</summary>
public static class ValidationRules
{
    /// <summary>Optional web link: empty, or an absolute http(s) address no longer than <paramref name="maxLength"/>.</summary>
    /// <typeparam name="T">The request.</typeparam>
    /// <param name="rule">The rule being built.</param>
    /// <param name="maxLength">Longest allowed.</param>
    /// <returns>The rule, for chaining.</returns>
    public static IRuleBuilderOptions<T, string?> OptionalWebUrl<T>(this IRuleBuilder<T, string?> rule, int maxLength) =>
        rule.MaximumLength(maxLength).WithMessage($"Keep the link under {maxLength} characters.")
            .Must(url => string.IsNullOrWhiteSpace(url) || TextRules.IsWebUrl(url.Trim()))
            .WithMessage("Please enter a full web address starting with https://.");

    /// <summary>Required web link.</summary>
    /// <typeparam name="T">The request.</typeparam>
    /// <param name="rule">The rule being built.</param>
    /// <param name="maxLength">Longest allowed.</param>
    /// <returns>The rule, for chaining.</returns>
    public static IRuleBuilderOptions<T, string> RequiredWebUrl<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule.NotEmpty().WithMessage("Please enter the web address.")
            .MaximumLength(maxLength).WithMessage($"Keep the link under {maxLength} characters.")
            .Must(url => TextRules.IsWebUrl(url?.Trim()))
            .WithMessage("Please enter a full web address starting with https://.");

    /// <summary>A list of short labels (tags, skills): not too many, none too long.</summary>
    /// <typeparam name="T">The request.</typeparam>
    /// <param name="rule">The rule being built.</param>
    /// <param name="maxItemLength">Longest label.</param>
    /// <param name="maxCount">Most labels.</param>
    /// <param name="what">What they are, for the messages (e.g. "tags").</param>
    /// <returns>The rule, for chaining.</returns>
    public static IRuleBuilderOptions<T, IReadOnlyList<string>?> Labels<T>(this IRuleBuilder<T, IReadOnlyList<string>?> rule,
                                                                         int maxItemLength, int maxCount, string what) =>
        rule.Must(list => list is null || list.Count(s => !string.IsNullOrWhiteSpace(s)) <= maxCount)
            .WithMessage($"Please use at most {maxCount} {what}.")
            .Must(list => list is null || list.All(s => s is null || s.Trim().Length <= maxItemLength))
            .WithMessage($"Each of the {what} must be under {maxItemLength} characters.");

    /// <summary>A reorder list: not empty, no Id twice, no empty Ids.</summary>
    /// <typeparam name="T">The request.</typeparam>
    /// <param name="rule">The rule being built.</param>
    /// <returns>The rule, for chaining.</returns>
    public static IRuleBuilderOptions<T, IReadOnlyList<Guid>> ReorderList<T>(this IRuleBuilder<T, IReadOnlyList<Guid>> rule) =>
        rule.NotEmpty().WithMessage("Send every item in its new order.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Each item can only appear once.")
            .Must(ids => ids.All(id => id != Guid.Empty)).WithMessage("The list contains an empty Id.");

    /// <summary>An uploaded picture: something was sent.</summary>
    /// <typeparam name="T">The request.</typeparam>
    /// <param name="rule">The rule being built.</param>
    /// <returns>The rule, for chaining.</returns>
    public static IRuleBuilderOptions<T, long> UploadSize<T>(this IRuleBuilder<T, long> rule) =>
        rule.GreaterThan(0).WithMessage("Please choose a picture to upload.");
}
