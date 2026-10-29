using FluentValidation;
using Zayun.Application.Common.Validation;
using Zayun.Application.Features.Awards.Commands;
using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Awards.Validators;

/// <summary>Checks <see cref="CreateAwardCommand"/>.</summary>
public sealed class CreateAwardCommandValidator : AbstractValidator<CreateAwardCommand>
{
    /// <summary>The rules.</summary>
    public CreateAwardCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please enter the award's name.")
                             .MaximumLength(Award.TitleMaxLength);
        RuleFor(x => x.AwardedBy).NotEmpty().WithMessage("Please say who gave it, or the event.")
                                 .MaximumLength(Award.AwardedByMaxLength);
        RuleFor(x => x.Year).InclusiveBetween(Publication.MinYear, Publication.MaxYear)
                            .WithMessage("Please enter a valid year.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Please say what it was for.")
                                   .MaximumLength(Award.DescriptionMaxLength);
        RuleFor(x => x.Url).OptionalWebUrl(Award.UrlMaxLength);
    }
}

/// <summary>Checks <see cref="UpdateAwardCommand"/>.</summary>
public sealed class UpdateAwardCommandValidator : AbstractValidator<UpdateAwardCommand>
{
    /// <summary>The rules.</summary>
    public UpdateAwardCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please enter the award's name.")
                             .MaximumLength(Award.TitleMaxLength);
        RuleFor(x => x.AwardedBy).NotEmpty().WithMessage("Please say who gave it, or the event.")
                                 .MaximumLength(Award.AwardedByMaxLength);
        RuleFor(x => x.Year).InclusiveBetween(Publication.MinYear, Publication.MaxYear)
                            .WithMessage("Please enter a valid year.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Please say what it was for.")
                                   .MaximumLength(Award.DescriptionMaxLength);
        RuleFor(x => x.Url).OptionalWebUrl(Award.UrlMaxLength);
        RuleFor(x => x.ImageAlt).MaximumLength(Award.ImageAltMaxLength);
    }
}

/// <summary>Checks <see cref="SetAwardImageCommand"/>.</summary>
public sealed class SetAwardImageCommandValidator : AbstractValidator<SetAwardImageCommand>
{
    /// <summary>The rules.</summary>
    public SetAwardImageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SizeBytes).UploadSize();
        RuleFor(x => x.AltText).NotEmpty().WithMessage("Please describe the picture for screen readers.")
                               .MaximumLength(Award.ImageAltMaxLength);
    }
}

/// <summary>Checks <see cref="ReorderAwardsCommand"/>.</summary>
public sealed class ReorderAwardsCommandValidator : AbstractValidator<ReorderAwardsCommand>
{
    /// <summary>The rules.</summary>
    public ReorderAwardsCommandValidator() => RuleFor(x => x.Ids).ReorderList();
}
