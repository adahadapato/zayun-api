namespace Zayun.Domain.Interfaces;

/// <summary>
/// Saves all changes made through the repositories in one go.
/// Implemented by ZayunDbContext in Persistence.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Saves every pending change in one database transaction.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// For the rare case where something else also saves on its own (e.g. Identity's
    /// UserManager at sign-up), so both can be committed or rolled back together.
    /// </summary>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}