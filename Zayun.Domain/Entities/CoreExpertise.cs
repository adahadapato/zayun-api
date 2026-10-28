using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// One item in the "Core expertise" list on the Skills page, e.g. "Explainable AI".
/// </summary>
/// <remarks>Names are unique (ignoring case); the handler checks against the repository.</remarks>
public class CoreExpertise : ContentEntity
{
    /// <summary>Longest name.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Required by EF Core.</summary>
    protected CoreExpertise() { }

    private CoreExpertise(Guid id, int displayOrder, bool isPublished) : base(id, displayOrder, isPublished) { }

    /// <summary>The expertise, as shown.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Adds an item.</summary>
    /// <param name="name">The expertise.</param>
    /// <param name="displayOrder">Its place in the list.</param>
    /// <param name="isPublished">Shown straight away, or kept as a draft.</param>
    /// <returns>The new item.</returns>
    /// <exception cref="ArgumentException">An empty name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A name that's too long.</exception>
    public static CoreExpertise Create(string name, int displayOrder, bool isPublished)
    {
        var item = new CoreExpertise(Guid.CreateVersion7(), displayOrder, isPublished);
        item.Rename(name);
        return item;
    }

    /// <summary>Changes the name.</summary>
    /// <param name="name">The expertise.</param>
    /// <exception cref="ArgumentException">An empty name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A name that's too long.</exception>
    public void Rename(string name) => Name = TextRules.Required(name, NameMaxLength, nameof(name));
}
