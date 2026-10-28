using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Interceptors;

/// <summary>
/// Fills the audit columns on every save: <c>CreatedAt</c> for new rows, <c>UpdatedAt</c> for changed rows. Domain code
/// never sets them (their setters are private), so the times always come from one clock.
/// </summary>
/// <remarks>
/// <para>Uses <see cref="TimeProvider"/>, so tests can freeze time.</para>
/// <para>A change to only an owned value (e.g. a presentation's new poster, stored as columns of the presentation) also counts as a change
/// to its owner, so the owner's <c>UpdatedAt</c> moves too.</para>
/// </remarks>
public sealed class AuditInterceptor(TimeProvider time) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>Sets CreatedAt / UpdatedAt on every Domain entity being added or changed.</summary>
    /// <param name="context">The context being saved; null for some EF internals.</param>
    private void Stamp(DbContext? context)
    {
        if (context is null) return;
        var now = time.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<Entity<Guid>>())
        {
            if (entry.State == EntityState.Added)
                entry.Property(e => e.CreatedAt).CurrentValue = now;
            else if (entry.State == EntityState.Modified || HasChangedOwnedValue(entry))
                entry.Property(e => e.UpdatedAt).CurrentValue = now;
        }
    }

    /// <summary>True when one of the entity's owned values (image, document file…) was added, changed or removed.</summary>
    private static bool HasChangedOwnedValue(EntityEntry entry) =>
        entry.References.Any(r => r.TargetEntry is { } target
                                  && target.Metadata.IsOwned()
                                  && target.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
}