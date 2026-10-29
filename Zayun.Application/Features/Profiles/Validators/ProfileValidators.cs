using FluentValidation;
using Zayun.Application.Common.Validation;
using Zayun.Application.Features.Profiles.Commands;
using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Profiles.Validators;

/// <summary>Checks <see cref="CreateProfileCommand"/>.</summary>
public sealed class CreateProfileCommandValidator : AbstractValidator<CreateProfileCommand>
{
    /// <summary>The rules.</summary>
    public CreateProfileCommandValidator()
    {
        RuleFor(x => x.Platform).IsInEnum().WithMessage("Please choose the site.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter the name shown, e.g. ORCID.")
                            .MaximumLength(Profile.NameMaxLength);
        RuleFor(x => x.Url).RequiredWebUrl(Profile.UrlMaxLength);
        RuleFor(x => x.Label).MaximumLength(Profile.LabelMaxLength);
    }
}

/// <summary>Checks <see cref="UpdateProfileCommand"/>.</summary>
public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    /// <summary>The rules.</summary>
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Platform).IsInEnum().WithMessage("Please choose the site.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter the name shown, e.g. ORCID.")
                            .MaximumLength(Profile.NameMaxLength);
        RuleFor(x => x.Url).RequiredWebUrl(Profile.UrlMaxLength);
        RuleFor(x => x.Label).MaximumLength(Profile.LabelMaxLength);
    }
}

/// <summary>Checks <see cref="ReorderProfilesCommand"/>.</summary>
public sealed class ReorderProfilesCommandValidator : AbstractValidator<ReorderProfilesCommand>
{
    /// <summary>The rules.</summary>
    public ReorderProfilesCommandValidator() => RuleFor(x => x.Ids).ReorderList();
}
