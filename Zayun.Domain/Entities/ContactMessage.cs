using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// A message sent with the website's Contact form, kept in the admin inbox so nothing is lost if an email goes
/// astray. Answers go out by email; here the admin only marks it Replied or Closed, with an optional private note.
/// </summary>
/// <remarks>Bad values are checked by the validators first, so they THROW here.</remarks>
public class ContactMessage : Entity<Guid>
{
    /// <summary>Longest name.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Longest email address.</summary>
    public const int EmailMaxLength = 256;

    /// <summary>Longest organisation (company, university…).</summary>
    public const int OrganisationMaxLength = 150;

    /// <summary>Shortest message (stops "hi").</summary>
    public const int MessageMinLength = 10;

    /// <summary>Longest message.</summary>
    public const int MessageMaxLength = 4000;

    /// <summary>Longest admin note.</summary>
    public const int NoteMaxLength = 500;

    /// <summary>Required by EF Core.</summary>
    protected ContactMessage() { }

    private ContactMessage(Guid id) : base(id) { }

    /// <summary>Who wrote, as they typed it.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Where to answer.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Where they're from, if they said.</summary>
    public string? Organisation { get; private set; }

    /// <summary>What it's about.</summary>
    public ContactTopic Topic { get; private set; }

    /// <summary>The message itself (plain text).</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>New, Replied or Closed.</summary>
    public ContactMessageStatus Status { get; private set; } = ContactMessageStatus.New;

    /// <summary>A private note, e.g. "Sent the paper's PDF".</summary>
    public string? StaffNote { get; private set; }

    /// <summary>When the status was last changed.</summary>
    public DateTime? HandledAtUtc { get; private set; }

    /// <summary>The admin who last changed the status.</summary>
    public Guid? HandledByUserId { get; private set; }

    /// <summary>A new message from the Contact form.</summary>
    /// <param name="name">Not empty, at most <see cref="NameMaxLength"/> characters.</param>
    /// <param name="email">Not empty, at most <see cref="EmailMaxLength"/> characters.</param>
    /// <param name="organisation">Optional, at most <see cref="OrganisationMaxLength"/> characters.</param>
    /// <param name="topic">What it's about.</param>
    /// <param name="message"><see cref="MessageMinLength"/> to <see cref="MessageMaxLength"/> characters.</param>
    /// <returns>The message, status New.</returns>
    /// <exception cref="ArgumentException">An empty name, email or message, or an unknown topic.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long or too short.</exception>
    public static ContactMessage Create(string name, string email, string? organisation, ContactTopic topic,
                                        string message)
    {
        if (!Enum.IsDefined(topic)) throw new ArgumentException("Unknown topic.", nameof(topic));

        var cleanMessage = TextRules.Required(message, MessageMaxLength, nameof(message));
        ArgumentOutOfRangeException.ThrowIfLessThan(cleanMessage.Length, MessageMinLength, nameof(message));

        return new ContactMessage(Guid.CreateVersion7())
        {
            Name = TextRules.Required(name, NameMaxLength, nameof(name)),
            Email = TextRules.Required(email, EmailMaxLength, nameof(email)),
            Organisation = TextRules.Optional(organisation, OrganisationMaxLength, nameof(organisation)),
            Topic = topic,
            Message = cleanMessage
        };
    }

    /// <summary>The admin marks the message (Replied, Closed, or back to New) and may leave a note.</summary>
    /// <param name="status">The new status.</param>
    /// <param name="note">Optional note; null or empty removes it.</param>
    /// <param name="adminUserId">Who did it.</param>
    /// <param name="utcNow">When.</param>
    /// <exception cref="ArgumentException">Unknown status.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A note that's too long.</exception>
    public void SetStatus(ContactMessageStatus status, string? note, Guid adminUserId, DateTime utcNow)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentException("Unknown status.", nameof(status));

        Status = status;
        StaffNote = TextRules.Optional(note, NoteMaxLength, nameof(note));
        HandledAtUtc = utcNow;
        HandledByUserId = adminUserId;
    }

    /// <summary>Removes it from the inbox (soft delete).</summary>
    /// <param name="utcNow">When.</param>
    public void Delete(DateTime utcNow) => SoftDelete(utcNow);
}
