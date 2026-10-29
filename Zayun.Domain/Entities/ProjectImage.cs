using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Text;
using Zayun.Domain.ValueObjects;

namespace Zayun.Domain.Entities;

/// <summary>
/// One picture of a project (a screenshot, a dashboard, a phone screen). The file lives in storage; this row records
/// where it is, its details and how it's shown. Belongs to a <see cref="Project"/> and is only created or changed
/// through it.
/// </summary>
public class ProjectImage : Entity<Guid>
{
    /// <summary>Longest alt text (column size).</summary>
    public const int AltTextMaxLength = 300;

    /// <summary>Longest caption, e.g. "Admin Platform" or "Seller" (column size).</summary>
    public const int LabelMaxLength = 60;

    /// <summary>Required by EF Core.</summary>
    protected ProjectImage() { }

    /// <summary>Records a processed picture for a project.</summary>
    /// <param name="imageId">The Id it was stored under (also used in its file path).</param>
    /// <param name="projectId">The project.</param>
    /// <param name="file">The stored image (already through the image pipeline).</param>
    /// <param name="altText">Text for screen readers.</param>
    /// <param name="label">Optional caption.</param>
    /// <param name="kind">How the card uses it.</param>
    /// <param name="displayOrder">Position among the project's pictures (0 = first).</param>
    /// <exception cref="ArgumentException">Empty alt text, or an unknown kind.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text longer than its column.</exception>
    internal ProjectImage(Guid imageId, Guid projectId, ImageFile file, string altText, string? label,
                          ProjectImageKind kind, int displayOrder)
        : base(imageId)
    {
        ArgumentNullException.ThrowIfNull(file);
        ProjectId = projectId;
        File = file;
        DisplayOrder = displayOrder;
        Change(altText, label, kind);
    }

    /// <summary>The project this picture belongs to.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Path, thumbnail, type, size, width, height and hash of the stored file.</summary>
    public ImageFile File { get; private set; } = null!;

    /// <summary>Read out by screen readers, e.g. "RCCG Hope House public-facing website".</summary>
    public string AltText { get; private set; } = string.Empty;

    /// <summary>Caption shown on the card, e.g. "Public Website"; null for none.</summary>
    public string? Label { get; private set; }

    /// <summary>Primary, Secondary or Gallery.</summary>
    public ProjectImageKind Kind { get; private set; }

    /// <summary>Lower numbers are shown first.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Changes how the picture is described and used (through <see cref="Project.UpdateImage"/>).</summary>
    /// <exception cref="ArgumentException">Empty alt text, or an unknown kind.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text longer than its column.</exception>
    internal void Change(string altText, string? label, ProjectImageKind kind)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentException("Unknown picture kind.", nameof(kind));
        AltText = TextRules.Required(altText, AltTextMaxLength, nameof(altText));
        Label = TextRules.Optional(label, LabelMaxLength, nameof(label));
        Kind = kind;
    }

    internal void SetDisplayOrder(int order) => DisplayOrder = order;

    /// <summary>Points the picture at a new stored file (through <see cref="Project.ReplaceImageFile"/>).</summary>
    internal void ReplaceFile(ImageFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        File = file;
    }
}
