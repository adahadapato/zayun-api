using FluentValidation;
using Zayun.Application.Features.Education.Commands;
using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Education.Validators;

/// <summary>The checks a new and an edited qualification share, with friendly messages for the admin form.</summary>
/// <typeparam name="T">The command.</typeparam>
public abstract class QualificationFieldsValidator<T> : AbstractValidator<T> where T : IQualificationFields
{
    /// <summary>The rules.</summary>
    protected QualificationFieldsValidator()
    {
        RuleFor(x => x.Degree).NotEmpty().WithMessage("Please give the degree or award, e.g. \"B.Sc.\".")
                              .MaximumLength(Qualification.DegreeMaxLength);
        RuleFor(x => x.Subject).NotEmpty().WithMessage("Please give the subject.")
                               .MaximumLength(Qualification.SubjectMaxLength);
        RuleFor(x => x.Institution).NotEmpty().WithMessage("Please say where you studied.")
                                   .MaximumLength(Qualification.InstitutionMaxLength);
        RuleFor(x => x.Location).MaximumLength(Qualification.LocationMaxLength);
        RuleFor(x => x.Year).InclusiveBetween(Qualification.MinYear, Qualification.MaxYear).When(x => x.Year is not null)
                            .WithMessage($"Please give the year ({Qualification.MinYear}–{Qualification.MaxYear}), or leave it empty while it's in progress.");
        RuleFor(x => x.Note).MaximumLength(Qualification.NoteMaxLength);
    }
}

/// <summary>Checks <see cref="CreateQualificationCommand"/>.</summary>
public sealed class CreateQualificationCommandValidator : QualificationFieldsValidator<CreateQualificationCommand>;

/// <summary>Checks <see cref="UpdateQualificationCommand"/>.</summary>
public sealed class UpdateQualificationCommandValidator : QualificationFieldsValidator<UpdateQualificationCommand>
{
    /// <summary>The rules (the shared ones, plus the Id).</summary>
    public UpdateQualificationCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
