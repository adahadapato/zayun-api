using FluentValidation;
using Zayun.Application.Common.Validation;
using Zayun.Application.Features.Skills.Commands;
using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Skills.Validators;

/// <summary>Checks <see cref="CreateSkillGroupCommand"/>.</summary>
public sealed class CreateSkillGroupCommandValidator : AbstractValidator<CreateSkillGroupCommand>
{
    /// <summary>The rules.</summary>
    public CreateSkillGroupCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please give the group a title.")
                             .MaximumLength(SkillGroup.TitleMaxLength);
        RuleFor(x => x.Description).NotEmpty().WithMessage("Please describe this group of skills.")
                                   .MaximumLength(SkillGroup.DescriptionMaxLength);
        RuleFor(x => x.Skills).Labels(SkillGroup.SkillMaxLength, SkillGroup.MaxSkills, "skills");
    }
}

/// <summary>Checks <see cref="UpdateSkillGroupCommand"/>.</summary>
public sealed class UpdateSkillGroupCommandValidator : AbstractValidator<UpdateSkillGroupCommand>
{
    /// <summary>The rules.</summary>
    public UpdateSkillGroupCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please give the group a title.")
                             .MaximumLength(SkillGroup.TitleMaxLength);
        RuleFor(x => x.Description).NotEmpty().WithMessage("Please describe this group of skills.")
                                   .MaximumLength(SkillGroup.DescriptionMaxLength);
        RuleFor(x => x.Skills).Labels(SkillGroup.SkillMaxLength, SkillGroup.MaxSkills, "skills");
    }
}

/// <summary>Checks <see cref="ReorderSkillGroupsCommand"/>.</summary>
public sealed class ReorderSkillGroupsCommandValidator : AbstractValidator<ReorderSkillGroupsCommand>
{
    /// <summary>The rules.</summary>
    public ReorderSkillGroupsCommandValidator() => RuleFor(x => x.Ids).ReorderList();
}
