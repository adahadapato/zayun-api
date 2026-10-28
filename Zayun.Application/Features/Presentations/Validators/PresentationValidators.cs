using FluentValidation;
using Zayun.Application.Common.Validation;
using Zayun.Application.Features.Presentations.Commands;
using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Presentations.Validators;

/// <summary>Checks <see cref="CreatePresentationCommand"/>.</summary>
public sealed class CreatePresentationCommandValidator : AbstractValidator<CreatePresentationCommand>
{
    /// <summary>The rules.</summary>
    public CreatePresentationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please enter what was presented.")
                             .MaximumLength(Presentation.TitleMaxLength);
        RuleFor(x => x.EventName).NotEmpty().WithMessage("Please enter the event.")
                                 .MaximumLength(Presentation.EventMaxLength);
        RuleFor(x => x.Year).InclusiveBetween(Publication.MinYear, Publication.MaxYear)
                            .WithMessage("Please enter a valid year.");
        RuleFor(x => x.Type).IsInEnum().WithMessage("Please choose the type of presentation.");
    }
}

/// <summary>Checks <see cref="UpdatePresentationCommand"/>.</summary>
public sealed class UpdatePresentationCommandValidator : AbstractValidator<UpdatePresentationCommand>
{
    /// <summary>The rules.</summary>
    public UpdatePresentationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please enter what was presented.")
                             .MaximumLength(Presentation.TitleMaxLength);
        RuleFor(x => x.EventName).NotEmpty().WithMessage("Please enter the event.")
                                 .MaximumLength(Presentation.EventMaxLength);
        RuleFor(x => x.Year).InclusiveBetween(Publication.MinYear, Publication.MaxYear)
                            .WithMessage("Please enter a valid year.");
        RuleFor(x => x.Type).IsInEnum().WithMessage("Please choose the type of presentation.");
        RuleFor(x => x.ImageAlt).MaximumLength(Presentation.ImageAltMaxLength);
    }
}

/// <summary>Checks <see cref="SetPresentationImageCommand"/>.</summary>
public sealed class SetPresentationImageCommandValidator : AbstractValidator<SetPresentationImageCommand>
{
    /// <summary>The rules.</summary>
    public SetPresentationImageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SizeBytes).UploadSize();
        RuleFor(x => x.AltText).NotEmpty().WithMessage("Please describe the picture for screen readers.")
                               .MaximumLength(Presentation.ImageAltMaxLength);
    }
}

/// <summary>Checks <see cref="ReorderPresentationsCommand"/>.</summary>
public sealed class ReorderPresentationsCommandValidator : AbstractValidator<ReorderPresentationsCommand>
{
    /// <summary>The rules.</summary>
    public ReorderPresentationsCommandValidator() => RuleFor(x => x.Ids).ReorderList();
}
