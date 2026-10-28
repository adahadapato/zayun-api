using FluentValidation;
using Zayun.Application.Common.Validation;
using Zayun.Application.Features.Publications.Commands;
using Zayun.Application.Features.Publications.Dtos;
using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Publications.Validators;

/// <summary>Checks one link of a publication.</summary>
public sealed class PublicationLinkValidator : AbstractValidator<PublicationLinkDto>
{
    /// <summary>The rules.</summary>
    public PublicationLinkValidator()
    {
        RuleFor(x => x.Label).NotEmpty().WithMessage("Please give the link a label, e.g. \"View on IEEE Xplore\".")
                             .MaximumLength(PublicationLink.LabelMaxLength)
                             .WithMessage($"Keep the link label under {PublicationLink.LabelMaxLength} characters.");
        RuleFor(x => x.Url).RequiredWebUrl(PublicationLink.UrlMaxLength);
    }
}

/// <summary>Checks <see cref="CreatePublicationCommand"/>.</summary>
public sealed class CreatePublicationCommandValidator : AbstractValidator<CreatePublicationCommand>
{
    /// <summary>The rules.</summary>
    public CreatePublicationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please enter the title.")
                             .MaximumLength(Publication.TitleMaxLength);
        RuleFor(x => x.Authors).NotEmpty().WithMessage("Please list the authors.")
                               .MaximumLength(Publication.AuthorsMaxLength);
        RuleFor(x => x.Venue).NotEmpty().WithMessage("Please say where it was published.")
                             .MaximumLength(Publication.VenueMaxLength);
        RuleFor(x => x.Year).InclusiveBetween(Publication.MinYear, Publication.MaxYear)
                            .WithMessage("Please enter a valid year.");
        RuleFor(x => x.Type).IsInEnum().WithMessage("Please choose the type of publication.");
        RuleFor(x => x.Award).MaximumLength(Publication.AwardMaxLength);
        RuleFor(x => x.Links).Must(l => l is null || l.Count <= Publication.MaxLinks)
                             .WithMessage($"Please add at most {Publication.MaxLinks} links.");
        RuleForEach(x => x.Links).SetValidator(new PublicationLinkValidator());
    }
}

/// <summary>Checks <see cref="UpdatePublicationCommand"/>.</summary>
public sealed class UpdatePublicationCommandValidator : AbstractValidator<UpdatePublicationCommand>
{
    /// <summary>The rules.</summary>
    public UpdatePublicationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please enter the title.")
                             .MaximumLength(Publication.TitleMaxLength);
        RuleFor(x => x.Authors).NotEmpty().WithMessage("Please list the authors.")
                               .MaximumLength(Publication.AuthorsMaxLength);
        RuleFor(x => x.Venue).NotEmpty().WithMessage("Please say where it was published.")
                             .MaximumLength(Publication.VenueMaxLength);
        RuleFor(x => x.Year).InclusiveBetween(Publication.MinYear, Publication.MaxYear)
                            .WithMessage("Please enter a valid year.");
        RuleFor(x => x.Type).IsInEnum().WithMessage("Please choose the type of publication.");
        RuleFor(x => x.Award).MaximumLength(Publication.AwardMaxLength);
        RuleFor(x => x.Links).Must(l => l is null || l.Count <= Publication.MaxLinks)
                             .WithMessage($"Please add at most {Publication.MaxLinks} links.");
        RuleForEach(x => x.Links).SetValidator(new PublicationLinkValidator());
    }
}

/// <summary>Checks <see cref="ReorderPublicationsCommand"/>.</summary>
public sealed class ReorderPublicationsCommandValidator : AbstractValidator<ReorderPublicationsCommand>
{
    /// <summary>The rules.</summary>
    public ReorderPublicationsCommandValidator() => RuleFor(x => x.Ids).ReorderList();
}
