using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Contact.Dtos;

/// <summary>One message in the admin inbox.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Name">Who wrote.</param>
/// <param name="Email">Where to answer.</param>
/// <param name="Organisation">Where they're from, if they said.</param>
/// <param name="Topic">What it's about.</param>
/// <param name="Message">The message.</param>
/// <param name="Status">New, Replied or Closed.</param>
/// <param name="StaffNote">The admin's private note.</param>
/// <param name="SentAt">When it arrived (UTC).</param>
/// <param name="HandledAt">When the status last changed (UTC).</param>
public sealed record ContactMessageDto(
    Guid Id,
    string Name,
    string Email,
    string? Organisation,
    ContactTopic Topic,
    string Message,
    ContactMessageStatus Status,
    string? StaffNote,
    DateTime SentAt,
    DateTime? HandledAt)
{
    /// <summary>Maps a message.</summary>
    /// <param name="m">The message.</param>
    /// <returns>The DTO.</returns>
    public static ContactMessageDto From(ContactMessage m) =>
        new(m.Id, m.Name, m.Email, m.Organisation, m.Topic, m.Message, m.Status, m.StaffNote, m.CreatedAt,
            m.HandledAtUtc);
}

/// <summary>One page of the inbox plus the counts for its tabs.</summary>
/// <param name="Messages">The page.</param>
/// <param name="NewCount">Messages not looked at yet.</param>
/// <param name="RepliedCount">Messages answered.</param>
/// <param name="ClosedCount">Messages done.</param>
public sealed record ContactInboxDto(
    PagedResult<ContactMessageDto> Messages,
    int NewCount,
    int RepliedCount,
    int ClosedCount);
