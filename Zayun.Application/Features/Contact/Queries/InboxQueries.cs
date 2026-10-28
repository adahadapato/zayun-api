using MediatR;
using Zayun.Application.Features.Contact.Dtos;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Contact.Queries;

/// <summary>Admin → Inbox: one page of messages, newest first, optionally one status, with the tab counts.</summary>
/// <param name="Status">Only this status; null = all.</param>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Messages per page.</param>
public sealed record GetContactMessagesQuery(
    ContactMessageStatus? Status = null,
    int Page = 1,
    int PageSize = PageRequest.DefaultPageSize) : IRequest<Result<ContactInboxDto>>;

/// <summary>Handles <see cref="GetContactMessagesQuery"/>.</summary>
public sealed class GetContactMessagesQueryHandler(IContactMessageRepository messages)
    : IRequestHandler<GetContactMessagesQuery, Result<ContactInboxDto>>
{
    /// <inheritdoc />
    public async Task<Result<ContactInboxDto>> Handle(GetContactMessagesQuery query, CancellationToken ct)
    {
        var page = new PageRequest { Page = query.Page, PageSize = query.PageSize };
        var paged = await messages.GetPageAsync(query.Status, page, ct);
        var counts = await messages.CountByStatusAsync(ct);

        var items = paged.Items.Select(ContactMessageDto.From).ToList();
        return new ContactInboxDto(
            new PagedResult<ContactMessageDto>(items, paged.TotalCount, paged.Page, paged.PageSize),
            counts.GetValueOrDefault(ContactMessageStatus.New),
            counts.GetValueOrDefault(ContactMessageStatus.Replied),
            counts.GetValueOrDefault(ContactMessageStatus.Closed));
    }
}

/// <summary>Admin → Inbox → one message.</summary>
/// <param name="Id">The message.</param>
public sealed record GetContactMessageQuery(Guid Id) : IRequest<Result<ContactMessageDto>>;

/// <summary>Handles <see cref="GetContactMessageQuery"/>.</summary>
public sealed class GetContactMessageQueryHandler(IContactMessageRepository messages)
    : IRequestHandler<GetContactMessageQuery, Result<ContactMessageDto>>
{
    /// <inheritdoc />
    public async Task<Result<ContactMessageDto>> Handle(GetContactMessageQuery query, CancellationToken ct)
    {
        var message = await messages.GetByIdAsync(query.Id, ct);
        return message is null ? ContactMessageErrors.NotFound : ContactMessageDto.From(message);
    }
}
