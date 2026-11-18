using FluentValidation;
using Zayun.Application.Common.Validation;
using Zayun.Application.Features.CareerRoles.Commands;
using Zayun.Domain.Entities;

namespace Zayun.Application.Features.CareerRoles.Validators;

/// <summary>The checks a new and an edited role share, with friendly messages for the admin form.</summary>
/// <typeparam name="T">The command.</typeparam>
public abstract class CareerRoleFieldsValidator<T> : AbstractValidator<T> where T : ICareerRoleFields
{
    /// <summary>The rules.</summary>
    protected CareerRoleFieldsValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Please choose the kind of role.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please give the job title.")
                             .MaximumLength(CareerRole.TitleMaxLength);
        RuleFor(x => x.Organisation).NotEmpty().WithMessage("Please say where (the organisation).")
                                    .MaximumLength(CareerRole.OrganisationMaxLength);
        RuleFor(x => x.Location).MaximumLength(CareerRole.LocationMaxLength);

        RuleFor(x => x.StartYear).InclusiveBetween(CareerRole.MinYear, CareerRole.MaxYear)
                                 .WithMessage($"Please give the start year ({CareerRole.MinYear}–{CareerRole.MaxYear}).");
        RuleFor(x => x.StartMonth).InclusiveBetween(1, 12).When(x => x.StartMonth is not null)
                                  .WithMessage("Please choose a month from the list.");
        RuleFor(x => x.EndYear).InclusiveBetween(CareerRole.MinYear, CareerRole.MaxYear).When(x => x.EndYear is not null)
                               .WithMessage($"Please give the end year ({CareerRole.MinYear}–{CareerRole.MaxYear}), or leave it empty for a current role.");
        RuleFor(x => x.EndMonth).InclusiveBetween(1, 12).When(x => x.EndMonth is not null)
                                .WithMessage("Please choose a month from the list.");
        RuleFor(x => x.EndMonth).Null().When(x => x.EndYear is null)
                                .WithMessage("Please give the end year too, or clear the end month.");
        RuleFor(x => x.EndYear)
            .Must((x, _) => !CareerRole.EndsBeforeStart(x.ToDetails()))
            .WithMessage("The role can't end before it starts.");

        RuleFor(x => x.Summary).MaximumLength(CareerRole.SummaryMaxLength);
        RuleFor(x => x.Highlights).Labels(CareerRole.HighlightMaxLength, CareerRole.MaxHighlights, "highlights");
    }
}

/// <summary>Checks <see cref="CreateCareerRoleCommand"/>.</summary>
public sealed class CreateCareerRoleCommandValidator : CareerRoleFieldsValidator<CreateCareerRoleCommand>;

/// <summary>Checks <see cref="UpdateCareerRoleCommand"/>.</summary>
public sealed class UpdateCareerRoleCommandValidator : CareerRoleFieldsValidator<UpdateCareerRoleCommand>
{
    /// <summary>The rules (the shared ones, plus the Id).</summary>
    public UpdateCareerRoleCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

