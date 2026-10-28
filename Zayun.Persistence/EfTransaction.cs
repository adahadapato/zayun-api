using Microsoft.EntityFrameworkCore.Storage;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence;

/// <summary>
/// <see cref="ITransaction"/> over EF Core's <see cref="IDbContextTransaction"/>, so Application can run several
/// saves (e.g. Identity's UserManager + our repositories at sign-up) as one unit without seeing EF Core types.
/// </summary>
/// <remarks>Disposing without <see cref="CommitAsync"/> rolls everything back.</remarks>
internal sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction
{
    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction.CommitAsync(cancellationToken);

    /// <inheritdoc />
    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        transaction.RollbackAsync(cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}