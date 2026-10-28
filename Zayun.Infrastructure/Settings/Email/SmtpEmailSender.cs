using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Zayun.Domain.Interfaces;

namespace Zayun.Infrastructure.Settings.Email;

/// <summary>
/// <see cref="IEmailSender"/> over SMTP (MailKit). One connection per email: Zayun sends very few.
/// </summary>
/// <remarks>Failures are logged and THROWN; callers decide whether an email failing matters (see
/// <see cref="IEmailSender"/>).</remarks>
internal sealed class SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
    : IEmailSender
{
    /// <inheritdoc />
    public Task SendAsync(string to, string toName, string subject, string htmlBody,
                          CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        var settings = options.Value;
        return SendCoreAsync(new MailboxAddress(toName, to), subject, htmlBody,
                             new MailboxAddress(settings.FromName, settings.OwnerInbox), cancellationToken);
    }

    /// <inheritdoc />
    public Task SendToOwnerAsync(string subject, string htmlBody, string replyToAddress, string replyToName,
                                 CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replyToAddress);
        var settings = options.Value;
        return SendCoreAsync(new MailboxAddress(settings.FromName, settings.OwnerInbox), subject, htmlBody,
                             new MailboxAddress(replyToName, replyToAddress), cancellationToken);
    }

    private async Task SendCoreAsync(MailboxAddress to, string subject, string htmlBody, MailboxAddress replyTo,
                                     CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);
        var settings = options.Value;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.ReplyTo.Add(replyTo);
        message.To.Add(to);
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(settings.Host, settings.Port, settings.Security, cancellationToken);

            if (!string.IsNullOrWhiteSpace(settings.Username))
                await client.AuthenticateAsync(settings.Username, settings.Password ?? string.Empty, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            logger.LogInformation("Email sent: {Subject}", subject);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Email failed: {Subject}", subject);
            throw;
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(quit: true, CancellationToken.None);
        }
    }
}

/// <summary>
/// <see cref="IEmailSender"/> used while <c>Email:Enabled</c> is false: writes each email to the log instead of
/// sending it.
/// </summary>
internal sealed class LogOnlyEmailSender(ILogger<LogOnlyEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    public Task SendAsync(string to, string toName, string subject, string htmlBody,
                          CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email (not sent, Email:Enabled is false) to {To}: {Subject}", to, subject);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendToOwnerAsync(string subject, string htmlBody, string replyToAddress, string replyToName,
                                 CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email (not sent, Email:Enabled is false) to the owner, reply-to {ReplyTo}: {Subject}",
                              replyToAddress, subject);
        return Task.CompletedTask;
    }
}
