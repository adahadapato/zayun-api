namespace Zayun.Domain.Entities;

/// <summary>
/// Base for everything shown on the website (projects, publications, presentations, experience, skills): each has a
/// place in its list and can be hidden from the site without being deleted.
/// </summary>
/// <remarks>
/// <para><b>Order:</b> lower <see cref="DisplayOrder"/> values are shown first. New items go to the end of their list;
/// the admin's drag-to-reorder sets every item's position in one go.</para>
/// <para><b>Published:</b> unpublished items are drafts, visible only in the admin area.</para>
/// <para><b>Delete</b> is a soft delete: the row stays in the table but disappears from every query.</para>
/// </remarks>
public abstract class ContentEntity : Entity<Guid>
{
    /// <summary>Required by EF Core.</summary>
    protected ContentEntity() { }

    /// <summary>Starts a new item.</summary>
    /// <param name="id">The new Id.</param>
    /// <param name="displayOrder">Its place in the list (0 or more).</param>
    /// <param name="isPublished">Shown on the website straight away, or kept as a draft.</param>
    /// <exception cref="ArgumentOutOfRangeException">A negative position.</exception>
    protected ContentEntity(Guid id, int displayOrder, bool isPublished) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(displayOrder);
        DisplayOrder = displayOrder;
        IsPublished = isPublished;
    }

    /// <summary>Lower numbers are shown first.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>True when the website shows it; false while it's a draft.</summary>
    public bool IsPublished { get; private set; }

    /// <summary>Shows it on the website.</summary>
    public void Publish() => IsPublished = true;

    /// <summary>Hides it from the website (it stays in the admin area).</summary>
    public void Unpublish() => IsPublished = false;

    /// <summary>Moves it to a new place in its list.</summary>
    /// <param name="displayOrder">The new position (0 or more).</param>
    /// <exception cref="ArgumentOutOfRangeException">A negative position.</exception>
    public void MoveTo(int displayOrder)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(displayOrder);
        DisplayOrder = displayOrder;
    }

    /// <summary>Removes it from the website and the admin lists (soft delete).</summary>
    /// <param name="utcNow">When.</param>
    public virtual void Delete(DateTime utcNow)
    {
        IsPublished = false;
        SoftDelete(utcNow);
    }
}
