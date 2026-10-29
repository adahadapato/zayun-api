using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves the owner's personal details (one row). Implemented in Persistence.</summary>
public interface IPersonalDetailsRepository : IRepository<PersonalDetails>
{
    /// <summary>The details, for update; null before they're first saved.</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<PersonalDetails?> GetAsync(CancellationToken cancellationToken = default);
}
