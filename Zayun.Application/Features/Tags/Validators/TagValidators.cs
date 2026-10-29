using FluentValidation;
using Zayun.Application.Features.Tags.Commands;
using Zayun.Application.Features.Tags.Queries;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Features.Tags.Validators;

/// <summary>The shared rule for a tag name.</summary>
internal static class TagRules
{
    public static IRuleBuilderOptions<T, string> TagName<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(n => TagNames.Clean(n).Length > 0).WithMessage("Please type a tag.")
            .Must(n => TagNames.Clean(n).Length <= TagNames.MaxLength)
            .WithMessage($"Keep tags under {TagNames.MaxLength} characters.");
}

/// <summary>Checks <see cref="CheckTagQuery"/>.</summary>
public sealed class CheckTagQueryValidator : AbstractValidator<CheckTagQuery>
{
    /// <summary>The rules.</summary>
    public CheckTagQueryValidator() => RuleFor(x => x.Name).TagName();
}

/// <summary>Checks <see cref="CreateTagCommand"/>.</summary>
public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    /// <summary>The rules.</summary>
    public CreateTagCommandValidator() => RuleFor(x => x.Name).TagName();
}

/// <summary>Checks <see cref="RenameTagCommand"/>.</summary>
public sealed class RenameTagCommandValidator : AbstractValidator<RenameTagCommand>
{
    /// <summary>The rules.</summary>
    public RenameTagCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).TagName();
    }
}

/// <summary>Checks <see cref="MergeTagCommand"/>.</summary>
public sealed class MergeTagCommandValidator : AbstractValidator<MergeTagCommand>
{
    /// <summary>The rules.</summary>
    public MergeTagCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.IntoTagId).NotEmpty().WithMessage("Choose the tag to merge into.");
    }
}

/// <summary>Checks <see cref="GetTagsQuery"/>.</summary>
public sealed class GetTagsQueryValidator : AbstractValidator<GetTagsQuery>
{
    /// <summary>The rules.</summary>
    public GetTagsQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(TagNames.MaxLength);
        RuleFor(x => x.Filter).IsInEnum();
        RuleFor(x => x.Sort).IsInEnum();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
