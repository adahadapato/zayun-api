using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves the shared tags. Implemented in Persistence.</summary>
public interface ITagRepository : IRepository<Tag>
{
    /// <summary>Every tag, by name (a portfolio has tens or a few hundred, so they're loaded together).</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
}
