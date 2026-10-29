using FluentValidation;
using Zayun.Application.Features.PersonalDetails.Commands;
using Zayun.Application.Features.PersonalDetails.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Features.PersonalDetails.Validators;

/// <summary>Checks one phone number.</summary>
public sealed class PhoneDtoValidator : AbstractValidator<PhoneDto>
{
    /// <summary>The rules.</summary>
    public PhoneDtoValidator()
    {
        RuleFor(x => x.Label).NotEmpty().WithMessage("Please label the number, e.g. UK or Mobile.")
                             .MaximumLength(ContactPhone.LabelMaxLength);
        RuleFor(x => x.Number).NotEmpty().WithMessage("Please enter the number.")
                              .MaximumLength(ContactPhone.NumberMaxLength)
                              .Must(PhoneNumbers.IsValid)
                              .WithMessage("Please enter a phone number, e.g. +44 7881 186690.");
    }
}

/// <summary>Checks one address.</summary>
public sealed class AddressDtoValidator : AbstractValidator<AddressDto>
{
    /// <summary>The rules.</summary>
    public AddressDtoValidator()
    {
        RuleFor(x => x.Label).NotEmpty().WithMessage("Please label the address, e.g. Home.")
                             .MaximumLength(PostalAddress.LabelMaxLength);
        RuleFor(x => x.Line1).NotEmpty().WithMessage("Please enter the first line.")
                             .MaximumLength(PostalAddress.LineMaxLength);
        RuleFor(x => x.Line2).MaximumLength(PostalAddress.LineMaxLength);
        RuleFor(x => x.City).NotEmpty().WithMessage("Please enter the town or city.")
                            .MaximumLength(PostalAddress.PlaceMaxLength);
        RuleFor(x => x.Region).MaximumLength(PostalAddress.PlaceMaxLength);
        RuleFor(x => x.Postcode).MaximumLength(PostalAddress.PostcodeMaxLength);
        RuleFor(x => x.Country).NotEmpty().WithMessage("Please enter the country.")
                               .MaximumLength(PostalAddress.PlaceMaxLength);
    }
}

/// <summary>Checks <see cref="SavePersonalDetailsCommand"/>.</summary>
public sealed class SavePersonalDetailsCommandValidator : AbstractValidator<SavePersonalDetailsCommand>
{
    /// <summary>The rules.</summary>
    public SavePersonalDetailsCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Please enter the name shown on the website.")
                                .MaximumLength(Domain.Entities.PersonalDetails.NameMaxLength);
        RuleFor(x => x.Headline).MaximumLength(Domain.Entities.PersonalDetails.HeadlineMaxLength);
        RuleFor(x => x.PublicEmail).MaximumLength(Domain.Entities.PersonalDetails.EmailMaxLength)
                                   .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.PublicEmail))
                                   .WithMessage("Please enter a valid email address.");
        RuleFor(x => x.Phones).Must(p => p is null || p.Count <= Domain.Entities.PersonalDetails.MaxPhones)
                              .WithMessage($"Please add at most {Domain.Entities.PersonalDetails.MaxPhones} phone numbers.");
        RuleForEach(x => x.Phones).SetValidator(new PhoneDtoValidator());
        RuleFor(x => x.Addresses).Must(a => a is null || a.Count <= Domain.Entities.PersonalDetails.MaxAddresses)
                                 .WithMessage($"Please add at most {Domain.Entities.PersonalDetails.MaxAddresses} addresses.");
        RuleForEach(x => x.Addresses).SetValidator(new AddressDtoValidator());
    }
}
