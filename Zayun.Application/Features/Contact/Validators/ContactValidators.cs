using FluentValidation;
using Zayun.Application.Features.Contact.Commands;
using Zayun.Application.Features.Contact.Queries;
using Zayun.Domain.Entities;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Contact.Validators;

/// <summary>Checks <see cref="SendContactMessageCommand"/>.</summary>
public sealed class SendContactMessageCommandValidator : AbstractValidator<SendContactMessageCommand>
{
    /// <summary>The rules.</summary>
    public SendContactMessageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter your name.")
                            .MaximumLength(ContactMessage.NameMaxLength);
        RuleFor(x => x.Email).NotEmpty().WithMessage("Please enter your email address so I can reply.")
                             .EmailAddress().WithMessage("Please enter a valid email address.")
                             .MaximumLength(ContactMessage.EmailMaxLength);
        RuleFor(x => x.Organisation).MaximumLength(ContactMessage.OrganisationMaxLength);
        RuleFor(x => x.Topic).IsInEnum().WithMessage("Please choose what your message is about.");
        RuleFor(x => x.Message).NotEmpty().WithMessage("Please write your message.")
                               .Must(m => m is null || m.Trim().Length >= ContactMessage.MessageMinLength)
                               .WithMessage($"Please write a little more (at least {ContactMessage.MessageMinLength} characters).")
                               .MaximumLength(ContactMessage.MessageMaxLength)
                               .WithMessage($"Please keep your message under {ContactMessage.MessageMaxLength:N0} characters.");
        RuleFor(x => x.Website).MaximumLength(200);
    }
}

/// <summary>Checks <see cref="UpdateContactMessageStatusCommand"/>.</summary>
public sealed class UpdateContactMessageStatusCommandValidator : AbstractValidator<UpdateContactMessageStatusCommand>
{
    /// <summary>The rules.</summary>
    public UpdateContactMessageStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(ContactMessage.NoteMaxLength)
                            .WithMessage($"Keep the note under {ContactMessage.NoteMaxLength} characters.");
    }
}

/// <summary>Checks <see cref="GetContactMessagesQuery"/>.</summary>
public sealed class GetContactMessagesQueryValidator : AbstractValidator<GetContactMessagesQuery>
{
    /// <summary>The rules.</summary>
    public GetContactMessagesQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
    }
}
