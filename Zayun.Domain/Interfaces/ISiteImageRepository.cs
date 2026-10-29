using Zayun.Domain.Entities;
using Zayun.Domain.Enums;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves the website's named pictures (e.g. the owner's photo). Implemented in Persistence.</summary>
public interface ISiteImageRepository : IRepository<SiteImage>
{
    /// <summary>Every picture (one per slot). Read-only.</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<IReadOnlyList<SiteImage>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>The picture in a slot, for update; null when the slot is empty.</summary>
    /// <param name="slot">The slot.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<SiteImage?> GetBySlotAsync(SiteImageSlot slot, CancellationToken cancellationToken = default);
}
