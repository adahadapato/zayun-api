using System.ComponentModel.DataAnnotations;
using MailKit.Security;
using Zayun.Domain.Constants;

namespace Zayun.Infrastructure.Settings.Email;

/// <summary>
/// The <c>"Email"</c> section (keys in <see cref="AppSettings.Email"/>): the SMTP server, who emails come from, and
/// the inbox Contact messages go to.
/// </summary>
/// <remarks>
/// <para>When <see cref="Enabled"/> is false, emails are only written to the log (handy in development); Contact
/// messages are still saved in the admin inbox either way.</para>
/// <para><see cref="Password"/> is a SECRET: User Secrets in development, the CI pipeline in production.</para>
/// </remarks>
public sealed class EmailSettings
{
    /// <summary>The section's name in appsettings.json.</summary>
    public const string SectionName = AppSettings.Email.Section;

    /// <summary>Send real emails (true) or only log them (false).</summary>
    public bool Enabled { get; set; }

    /// <summary>The name emails come from, e.g. "Zayun".</summary>
    [Required]
    public string FromName { get; set; } = string.Empty;

    /// <summary>The address emails come from.</summary>
    [Required, EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Where new Contact messages are sent.</summary>
    [Required, EmailAddress]
    public string OwnerInbox { get; set; } = string.Empty;

    /// <summary>The SMTP server.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>The SMTP port (587 for STARTTLS, 465 for SSL).</summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    /// <summary>How the connection is secured.</summary>
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    /// <summary>SMTP user name; empty for none.</summary>
    public string? Username { get; set; }

    /// <summary>SMTP password (SECRET).</summary>
    public string? Password { get; set; }
}
