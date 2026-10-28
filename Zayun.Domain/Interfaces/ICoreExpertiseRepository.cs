using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves the core expertise list. Implemented in Persistence.</summary>
public interface ICoreExpertiseRepository : IContentRepository<CoreExpertise>
{
    /// <summary>True when another item already has this name (ignoring case).</summary>
    /// <param name="name">The name.</param>
    /// <param name="exceptId">The item being renamed; null for a new one.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken = default);
}
