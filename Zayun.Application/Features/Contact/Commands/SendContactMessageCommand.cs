using System.Net;
using MediatR;
using Microsoft.Extensions.Logging;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Contact.Commands;

/// <summary>The website's Contact form.</summary>
/// <param name="Name">Who is writing.</param>
/// <param name="Email">Where to answer.</param>
/// <param name="Organisation">Optional company or university.</param>
/// <param name="Topic">What it's about.</param>
/// <param name="Message">The message.</param>
/// <param name="Website">A field hidden from people: only robots fill it in, so a value means spam.</param>
public sealed record SendContactMessageCommand(
    string Name,
    string Email,
    string? Organisation,
    ContactTopic Topic,
    string Message,
    string? Website = null) : IRequest<Result>;

/// <summary>Handles <see cref="SendContactMessageCommand"/>.</summary>
/// <remarks>
/// <para>The message is saved FIRST, so it's in the inbox even if email is down.</para>
/// <para>Then the owner gets an email whose Reply goes straight to the visitor, and the visitor gets a short
/// confirmation. A failed email is logged and never undoes the saved message.</para>
/// <para>Spam (the hidden field filled in) gets the same "sent" answer, so robots learn nothing, but nothing is
/// saved or emailed.</para>
/// </remarks>
public sealed class SendContactMessageCommandHandler(
    IContactMessageRepository messages,
    IUnitOfWork unitOfWork,
    IEmailSender email,
    ILogger<SendContactMessageCommandHandler> logger)
    : IRequestHandler<SendContactMessageCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(SendContactMessageCommand command, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(command.Website))
        {
            logger.LogInformation("Contact: dropped a message that filled the spam trap.");
            return Result.Success();
        }

        var message = ContactMessage.Create(command.Name, command.Email, command.Organisation, command.Topic,
                                            command.Message);
        messages.Add(message);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Contact: new {Topic} message {Id}.", message.Topic, message.Id);

        await TellTheOwnerAsync(message, ct);
        await ConfirmToSenderAsync(message, ct);
        return Result.Success();
    }

    private async Task TellTheOwnerAsync(ContactMessage m, CancellationToken ct)
    {
        try
        {
            var organisation = m.Organisation is null ? "" : $" from <b>{Enc(m.Organisation)}</b>";
            await email.SendToOwnerAsync(
                $"[Zayun contact] {TopicText(m.Topic)}: {m.Name}",
                $"<p><b>{Enc(m.Name)}</b> ({Enc(m.Email)}){organisation} wrote about " +
                $"<b>{TopicText(m.Topic).ToLowerInvariant()}</b>:</p>" +
                $"<blockquote style=\"white-space:pre-wrap\">{Enc(m.Message)}</blockquote>" +
                "<p>Press Reply to answer them, then mark it Replied or Closed in the admin inbox.</p>",
                m.Email, m.Name, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Contact: couldn't email the owner about message {Id}.", m.Id);
        }
    }

    private async Task ConfirmToSenderAsync(ContactMessage m, CancellationToken ct)
    {
        try
        {
            await email.SendAsync(m.Email, m.Name, "Thanks for your message",
                $"<p>Hi {Enc(m.Name)},</p>" +
                "<p>Thank you for getting in touch. Your message has arrived and I'll reply as soon as I can.</p>" +
                $"<p>Here's what you sent:</p><blockquote style=\"white-space:pre-wrap\">{Enc(m.Message)}</blockquote>" +
                "<p>If you'd like to add anything, just reply to this email.</p>",
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Contact: couldn't send the confirmation for message {Id}.", m.Id);
        }
    }

    private static string TopicText(ContactTopic topic) => topic switch
    {
        ContactTopic.Project => "A project",
        ContactTopic.Research => "Research",
        ContactTopic.Opportunity => "An opportunity",
        ContactTopic.Speaking => "Speaking",
        _ => "Something else"
    };

    private static string Enc(string text) => WebUtility.HtmlEncode(text);
}
