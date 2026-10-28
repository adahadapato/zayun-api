namespace Zayun.Domain.Interfaces;

/// <summary>
/// Sends emails. Implemented in Infrastructure (SMTP, settings in <c>Email</c>).
/// </summary>
/// <remarks>
/// <para><b>Failures throw.</b> Callers that must not fail because of an email (e.g. a Contact message that is already
/// saved) catch the exception and log it.</para>
/// <para>Bodies are HTML: callers must HTML-encode any text a visitor typed.</para>
/// </remarks>
public interface IEmailSender
{
    /// <summary>Sends an email to someone outside (e.g. "Thanks for your message").</summary>
    /// <param name="to">The recipient's address.</param>
    /// <param name="toName">The recipient's name.</param>
    /// <param name="subject">The subject line.</param>
    /// <param name="htmlBody">The body as HTML.</param>
    /// <param name="cancellationToken">Cancels sending.</param>
    Task SendAsync(string to, string toName, string subject, string htmlBody,
                   CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email to the site owner's inbox whose "Reply" goes straight to the visitor (a Contact message: press
    /// Reply and answer them).
    /// </summary>
    /// <param name="subject">The subject line.</param>
    /// <param name="htmlBody">The body as HTML.</param>
    /// <param name="replyToAddress">Where the reply goes (the visitor's address).</param>
    /// <param name="replyToName">The name shown with it.</param>
    /// <param name="cancellationToken">Cancels sending.</param>
    Task SendToOwnerAsync(string subject, string htmlBody, string replyToAddress, string replyToName,
                          CancellationToken cancellationToken = default);
}
