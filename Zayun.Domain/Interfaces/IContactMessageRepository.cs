using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Results;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves Contact messages (the admin inbox). Implemented in Persistence.</summary>
public interface IContactMessageRepository : IRepository<ContactMessage>
{
    /// <summary>
    /// The inbox: one page of messages, newest first, optionally only one status. Read-only.
    /// </summary>
    /// <param name="status">Only this status; null = all.</param>
    /// <param name="page">Which page and how many per page.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<PagedResult<ContactMessage>> GetPageAsync(ContactMessageStatus? status, PageRequest page,
                                                   CancellationToken cancellationToken = default);

    /// <summary>How many messages there are with each status (statuses with none are missing).</summary>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<IReadOnlyDictionary<ContactMessageStatus, int>> CountByStatusAsync(CancellationToken cancellationToken = default);
}
