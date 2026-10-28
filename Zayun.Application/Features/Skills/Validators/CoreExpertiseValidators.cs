using FluentValidation;
using Zayun.Application.Common.Validation;
using Zayun.Application.Features.Skills.Commands;
using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Skills.Validators;

/// <summary>Checks <see cref="CreateCoreExpertiseCommand"/>.</summary>
public sealed class CreateCoreExpertiseCommandValidator : AbstractValidator<CreateCoreExpertiseCommand>
{
    /// <summary>The rules.</summary>
    public CreateCoreExpertiseCommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter the expertise.")
                            .MaximumLength(CoreExpertise.NameMaxLength)
                            .WithMessage($"Keep it under {CoreExpertise.NameMaxLength} characters.");
}

/// <summary>Checks <see cref="RenameCoreExpertiseCommand"/>.</summary>
public sealed class RenameCoreExpertiseCommandValidator : AbstractValidator<RenameCoreExpertiseCommand>
{
    /// <summary>The rules.</summary>
    public RenameCoreExpertiseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter the expertise.")
                            .MaximumLength(CoreExpertise.NameMaxLength)
                            .WithMessage($"Keep it under {CoreExpertise.NameMaxLength} characters.");
    }
}

/// <summary>Checks <see cref="ReorderCoreExpertiseCommand"/>.</summary>
public sealed class ReorderCoreExpertiseCommandValidator : AbstractValidator<ReorderCoreExpertiseCommand>
{
    /// <summary>The rules.</summary>
    public ReorderCoreExpertiseCommandValidator() => RuleFor(x => x.Ids).ReorderList();
}
