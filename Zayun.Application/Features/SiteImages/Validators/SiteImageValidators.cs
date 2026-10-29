using FluentValidation;
using Zayun.Application.Common.Validation;
using Zayun.Application.Features.SiteImages.Commands;
using Zayun.Domain.Entities;

namespace Zayun.Application.Features.SiteImages.Validators;

/// <summary>Checks <see cref="SetSiteImageCommand"/>.</summary>
public sealed class SetSiteImageCommandValidator : AbstractValidator<SetSiteImageCommand>
{
    /// <summary>The rules.</summary>
    public SetSiteImageCommandValidator()
    {
        RuleFor(x => x.Slot).IsInEnum();
        RuleFor(x => x.SizeBytes).UploadSize();
        RuleFor(x => x.AltText).NotEmpty().WithMessage("Please describe the picture for screen readers.")
                               .MaximumLength(SiteImage.AltTextMaxLength);
    }
}

/// <summary>Checks <see cref="DescribeSiteImageCommand"/>.</summary>
public sealed class DescribeSiteImageCommandValidator : AbstractValidator<DescribeSiteImageCommand>
{
    /// <summary>The rules.</summary>
    public DescribeSiteImageCommandValidator()
    {
        RuleFor(x => x.Slot).IsInEnum();
        RuleFor(x => x.AltText).NotEmpty().WithMessage("Please describe the picture for screen readers.")
                               .MaximumLength(SiteImage.AltTextMaxLength);
    }
}
