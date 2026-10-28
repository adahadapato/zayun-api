using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>
/// What every repository can do. One repository per aggregate root, never per child entity
/// (no IProjectImageRepository: images are
/// changed through their root so its rules always apply).
/// Repositories never save: call IUnitOfWork.SaveChangesAsync() once at the end.
/// </summary>
public interface IRepository<TEntity> where TEntity : Entity<Guid>
{
    /// <summary>Loads the whole aggregate (root + its children), or null if not found / soft-deleted.</summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Marks a new aggregate to be inserted on the next save.</summary>
    void Add(TEntity entity);

    /// <summary>
    /// Marks an aggregate to be physically deleted on the next save.
    /// Most Zayun data is soft-deleted through entity methods instead (e.g. Project.Delete);
    /// use this only where a real delete is right (e.g. a contact message the admin wants gone for good).
    /// </summary>
    void Remove(TEntity entity);
}