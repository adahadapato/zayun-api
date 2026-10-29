using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// One name in the shared list of tags used by project tags, experience skills and skill-group skills, e.g.
/// "REST API". The list keeps those names consistent: "RESTful APIs", "rest api" and "REST APIs" all become "REST API".
/// </summary>
/// <remarks>
/// <para><see cref="Name"/> is shown as written ("TypeScript", "ASP.NET Core"); <see cref="NormalizedName"/> is the
/// UPPERCASE key (see <see cref="TagNames.Key"/>) and is unique.</para>
/// <para><see cref="Aliases"/> are other keys that mean this tag: old names after a rename or a merge, so typing the
/// old name later still finds it.</para>
/// <para>Projects, experience areas and skill groups keep the NAMES in their lists (so the website reads them
/// directly); renaming or merging a tag updates those lists.</para>
/// <para>Bad text is caught by the validators first, so the checks here THROW.</para>
/// </remarks>
public class Tag : Entity<Guid>
{
    /// <summary>Most aliases kept for one tag.</summary>
    public const int MaxAliases = 30;

    /// <summary>Required by EF Core.</summary>
    protected Tag() { }

    private Tag(Guid id) : base(id) { }

    /// <summary>The name as shown, e.g. "ASP.NET Core".</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The UPPERCASE key, e.g. "ASPNETCORE" (unique).</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Other keys that mean this tag (old names), e.g. "RESTFULAPI".</summary>
    public List<string> Aliases { get; private set; } = [];

    /// <summary>A new tag.</summary>
    /// <param name="name">The name as it should be shown.</param>
    /// <returns>The tag.</returns>
    /// <exception cref="ArgumentException">An empty name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A name longer than <see cref="TagNames.MaxLength"/>.</exception>
    public static Tag Create(string name)
    {
        var tag = new Tag(Guid.CreateVersion7());
        tag.SetName(name);
        return tag;
    }

    /// <summary>True when this key is the tag's key or one of its aliases.</summary>
    /// <param name="key">A key from <see cref="TagNames.Key"/>.</param>
    public bool Matches(string key) => NormalizedName == key || Aliases.Contains(key);

    /// <summary>Gives the tag a new name; the old one is kept as an alias so it is still recognised.</summary>
    /// <param name="name">The new name.</param>
    /// <returns>The old name (to replace in the lists that use it).</returns>
    /// <exception cref="ArgumentException">An empty name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A name longer than <see cref="TagNames.MaxLength"/>.</exception>
    public string Rename(string name)
    {
        var oldName = Name;
        var oldKey = NormalizedName;
        SetName(name);
        if (oldKey != NormalizedName) AddAlias(oldKey);
        return oldName;
    }

    /// <summary>Takes over another tag's key and aliases (Admin → Tags → Merge).</summary>
    /// <param name="other">The tag merged into this one (it is deleted afterwards).</param>
    public void Absorb(Tag other)
    {
        ArgumentNullException.ThrowIfNull(other);
        AddAlias(other.NormalizedName);
        foreach (var alias in other.Aliases) AddAlias(alias);
    }

    /// <summary>Records another key that means this tag.</summary>
    /// <param name="key">A key from <see cref="TagNames.Key"/>.</param>
    public void AddAlias(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key == NormalizedName || Aliases.Contains(key)) return;
        Aliases.Add(key);
        if (Aliases.Count > MaxAliases) Aliases.RemoveAt(0);   // oldest first
    }

    private void SetName(string name)
    {
        var clean = TextRules.Required(TagNames.Clean(name), TagNames.MaxLength, nameof(name));
        Name = clean;
        NormalizedName = TagNames.Key(clean);
        Aliases.Remove(NormalizedName);
    }
}
