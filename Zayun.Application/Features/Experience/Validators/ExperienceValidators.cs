using FluentValidation;
using Zayun.Application.Common.Validation;
using Zayun.Application.Features.Experience.Commands;
using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Experience.Validators;

/// <summary>Checks <see cref="CreateExperienceAreaCommand"/>.</summary>
public sealed class CreateExperienceAreaCommandValidator : AbstractValidator<CreateExperienceAreaCommand>
{
    /// <summary>The rules.</summary>
    public CreateExperienceAreaCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please give the area a title.")
                             .MaximumLength(ExperienceArea.TitleMaxLength);
        RuleFor(x => x.Description).NotEmpty().WithMessage("Please describe this experience.")
                                   .MaximumLength(ExperienceArea.DescriptionMaxLength);
        RuleFor(x => x.Skills).Labels(ExperienceArea.SkillMaxLength, ExperienceArea.MaxSkills, "skills");
    }
}

/// <summary>Checks <see cref="UpdateExperienceAreaCommand"/>.</summary>
public sealed class UpdateExperienceAreaCommandValidator : AbstractValidator<UpdateExperienceAreaCommand>
{
    /// <summary>The rules.</summary>
    public UpdateExperienceAreaCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please give the area a title.")
                             .MaximumLength(ExperienceArea.TitleMaxLength);
        RuleFor(x => x.Description).NotEmpty().WithMessage("Please describe this experience.")
                                   .MaximumLength(ExperienceArea.DescriptionMaxLength);
        RuleFor(x => x.Skills).Labels(ExperienceArea.SkillMaxLength, ExperienceArea.MaxSkills, "skills");
    }
}

/// <summary>Checks <see cref="ReorderExperienceAreasCommand"/>.</summary>
public sealed class ReorderExperienceAreasCommandValidator : AbstractValidator<ReorderExperienceAreasCommand>
{
    /// <summary>The rules.</summary>
    public ReorderExperienceAreasCommandValidator() => RuleFor(x => x.Ids).ReorderList();
}
