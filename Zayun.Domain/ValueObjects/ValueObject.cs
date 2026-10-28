namespace Zayun.Domain.ValueObjects;

/// <summary>
/// Represents the base class all value objects derive from.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    // CHANGED: nullable parameters (ValueObject?) so <Nullable>enable</Nullable> doesn't warn.
    public static bool operator ==(ValueObject? a, ValueObject? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(ValueObject? a, ValueObject? b) => !(a == b);

    /// <inheritdoc />
    // CHANGED: also checks the type, like Equals(object) already did.
    // Before, two different value object types with the same text counted as equal.
    public bool Equals(ValueObject? other) =>
        other is not null &&
        GetType() == other.GetType() &&
        GetAtomicValues().SequenceEqual(other.GetAtomicValues());

    /// <inheritdoc />
    // CHANGED: simply reuses Equals(ValueObject) so the two can never disagree.
    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hashCode = default;

        foreach (object? obj in GetAtomicValues())
        {
            hashCode.Add(obj);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Gets the atomic values of the value object.
    /// </summary>
    /// <returns>The collection of objects representing the value object values.</returns>
    // CHANGED: object? so a value object can have an optional part (e.g. a null middle name).
    protected abstract IEnumerable<object?> GetAtomicValues();
}