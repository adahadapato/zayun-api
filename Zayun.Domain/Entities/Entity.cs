namespace Zayun.Domain.Entities;

/// <summary>
/// Represents the base class that all entities derive from.
/// Supports EF Core proxies (lazy loading, change tracking).
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
{
    protected Entity(TId id)
        : this()
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        // Prevent default IDs
        if (typeof(TId) == typeof(Guid) && (Guid)(object)id == Guid.Empty)
            throw new ArgumentException("ID cannot be empty.", nameof(id));
        if (typeof(TId) == typeof(int) && (int)(object)id == 0)
            throw new ArgumentException("ID cannot be zero.", nameof(id));

        Id = id;
    }

    /// <summary>Required by EF Core and proxy generation.</summary>
    protected Entity()
    {
    }

    public virtual TId Id { get; protected set; } = default!;

    /// <summary>Determines whether this entity is transient (not yet persisted).</summary>
    public bool IsTransient() => Id == null || Id.Equals(default(TId));

    // CHANGED: nullable parameters so <Nullable>enable</Nullable> doesn't warn.
    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !(a == b);

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // CHANGED: two different objects with no Id yet are NOT the same entity.
        if (IsTransient() || other.IsTransient()) return false;

        // CHANGED: compare real types, so an EF proxy and the plain entity still match.
        if (GetUnproxiedType(this) != GetUnproxiedType(other)) return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => Id?.GetHashCode() * 41 ?? 0;

    /// <summary>
    /// EF Core lazy-loading proxies are generated subclasses ("Castle.Proxies.ProjectProxy : Project").
    /// Returns the real entity type. Checked by name, so Domain needs no EF Core reference.
    /// </summary>
    private static Type GetUnproxiedType(object obj)
    {
        var type = obj.GetType();
        return type.Namespace == "Castle.Proxies" && type.BaseType is not null ? type.BaseType : type;
    }

    // ---------- Audit ----------
    // Set automatically by Persistence when the entity is saved (never by Domain code).

    /// <summary>When the row was first saved (UTC).</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>When the row was last changed (UTC); null if never changed since creation.</summary>
    public DateTime? UpdatedAt { get; private set; }

    // ---------- Soft delete ----------
    // Deleted rows stay in the table (so nothing is lost by mistake) but are hidden from queries.

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Only entities themselves call this, from a meaningful method
    /// (e.g. Project.Delete). Not public on purpose.
    /// </summary>
    protected void SoftDelete(DateTime utcNow)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = utcNow;
    }
}