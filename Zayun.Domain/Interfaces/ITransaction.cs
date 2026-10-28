namespace Zayun.Domain.Interfaces;

/// <summary>
/// A database transaction, without exposing EF Core's own types outside Persistence.
/// Disposing without committing rolls it back.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}