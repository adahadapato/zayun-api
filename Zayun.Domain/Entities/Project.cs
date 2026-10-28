using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.Shared.Text;
using Zayun.Domain.ValueObjects;

namespace Zayun.Domain.Entities;

/// <summary>
/// A piece of work shown on the Projects page: what it is, what it was built with, where to see it, and its pictures.
/// </summary>
/// <remarks>
/// <para><b>Web address:</b> every project has a unique <see cref="Slug"/> (e.g. <c>rccg-hope-house</c>). Uniqueness
/// is checked by the handler against the repository, because it needs the other projects.</para>
/// <para><b>Pictures</b> are children (<see cref="ProjectImage"/>) and only change through the methods here, so the
/// rules (at most <see cref="MaxImages"/>, one Primary, one Secondary, no duplicates) always apply.</para>
/// <para>Bad text is caught by the validators first, so the checks here THROW. Business rules return
/// <see cref="Result"/>.</para>
/// </remarks>
public class Project : ContentEntity
{
    /// <summary>Longest title.</summary>
    public const int TitleMaxLength = 150;

    /// <summary>Longest subtitle, e.g. "AI Clinical Decision Support".</summary>
    public const int SubtitleMaxLength = 200;

    /// <summary>Longest description.</summary>
    public const int DescriptionMaxLength = 4000;

    /// <summary>Longest label, e.g. "Full-Stack Engineering" or "Brunel University London".</summary>
    public const int LabelMaxLength = 100;

    /// <summary>Longest picture-area label, e.g. "Full-Stack Platform".</summary>
    public const int VisualLabelMaxLength = 100;

    /// <summary>Longest link.</summary>
    public const int UrlMaxLength = 500;

    /// <summary>Longest tag, e.g. "Clinical Decision Support".</summary>
    public const int TagMaxLength = 60;

    /// <summary>Most tags.</summary>
    public const int MaxTags = 20;

    /// <summary>Most pictures.</summary>
    public const int MaxImages = 12;

    private readonly List<ProjectImage> _images = [];

    /// <summary>Required by EF Core.</summary>
    protected Project() { }

    private Project(Guid id, int displayOrder, bool isPublished) : base(id, displayOrder, isPublished) { }

    /// <summary>The project's name, e.g. "RCCG Hope House".</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Its web address, unique among projects.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>One line under the title.</summary>
    public string Subtitle { get; private set; } = string.Empty;

    /// <summary>What it is and does (plain text).</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Featured or Engineering.</summary>
    public ProjectCategory Category { get; private set; }

    /// <summary>The small label on the card, e.g. "AI Innovation".</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>Label over the picture area, e.g. "Research Output"; null for the default.</summary>
    public string? VisualLabel { get; private set; }

    /// <summary>The source code, when it's public.</summary>
    public string? GithubUrl { get; private set; }

    /// <summary>The live site, if there is one.</summary>
    public string? LiveUrl { get; private set; }

    /// <summary>True when the code is private (the card says so instead of linking to it).</summary>
    public bool IsPrivateRepo { get; private set; }

    /// <summary>Technologies and topics, in the order shown.</summary>
    public List<string> Tags { get; private set; } = [];

    /// <summary>The pictures, in no particular order: sort by <see cref="ProjectImage.DisplayOrder"/>.</summary>
    public virtual IReadOnlyCollection<ProjectImage> Images => _images.AsReadOnly();

    /// <summary>Adds a project.</summary>
    /// <param name="details">Everything about it except its place and pictures.</param>
    /// <param name="slug">Its web address, already checked by <see cref="Slugs.IsValid"/> and for uniqueness.</param>
    /// <param name="displayOrder">Its place in the list.</param>
    /// <param name="isPublished">Shown straight away, or kept as a draft.</param>
    /// <returns>The new project.</returns>
    /// <exception cref="ArgumentException">Missing text, a bad link or slug, or an unknown category.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or too many tags.</exception>
    public static Project Create(ProjectDetails details, string slug, int displayOrder, bool isPublished)
    {
        var project = new Project(Guid.CreateVersion7(), displayOrder, isPublished);
        project.Update(details, slug);
        return project;
    }

    /// <summary>Changes everything about the project except its place and pictures.</summary>
    /// <param name="details">The new details.</param>
    /// <param name="slug">The web address, already checked by <see cref="Slugs.IsValid"/> and for uniqueness.</param>
    /// <exception cref="ArgumentException">Missing text, a bad link or slug, or an unknown category.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or too many tags.</exception>
    public void Update(ProjectDetails details, string slug)
    {
        ArgumentNullException.ThrowIfNull(details);
        if (!Enum.IsDefined(details.Category))
            throw new ArgumentException("Unknown project category.", nameof(details));
        if (!Slugs.IsValid(slug))
            throw new ArgumentException("Not a valid web address.", nameof(slug));

        Title = TextRules.Required(details.Title, TitleMaxLength, nameof(details.Title));
        Slug = slug;
        Subtitle = TextRules.Required(details.Subtitle, SubtitleMaxLength, nameof(details.Subtitle));
        Description = TextRules.Required(details.Description, DescriptionMaxLength, nameof(details.Description));
        Category = details.Category;
        Label = TextRules.Required(details.Label, LabelMaxLength, nameof(details.Label));
        VisualLabel = TextRules.Optional(details.VisualLabel, VisualLabelMaxLength, nameof(details.VisualLabel));
        GithubUrl = TextRules.OptionalUrl(details.GithubUrl, UrlMaxLength, nameof(details.GithubUrl));
        LiveUrl = TextRules.OptionalUrl(details.LiveUrl, UrlMaxLength, nameof(details.LiveUrl));
        IsPrivateRepo = details.IsPrivateRepo;
        Tags = TextRules.Labels(details.Tags, TagMaxLength, MaxTags, nameof(details.Tags));
    }

    // ───────────────────────────── Pictures ─────────────────────────────

    /// <summary>Adds a processed picture at the end of the project's pictures.</summary>
    /// <param name="imageId">The Id the file was stored under.</param>
    /// <param name="file">The stored image.</param>
    /// <param name="altText">Text for screen readers.</param>
    /// <param name="label">Optional caption.</param>
    /// <param name="kind">How the card uses it.</param>
    /// <returns>The new picture; or TooManyImages, DuplicateImage or ImageKindTaken.</returns>
    /// <exception cref="ArgumentException">Empty alt text, or an unknown kind.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text longer than its column.</exception>
    public Result<ProjectImage> AddImage(Guid imageId, ImageFile file, string altText, string? label,
                                         ProjectImageKind kind)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (_images.Count >= MaxImages) return ProjectErrors.TooManyImages;
        if (_images.Any(i => i.File.Hash == file.Hash)) return ProjectErrors.DuplicateImage;
        if (IsSingleKindTaken(kind, exceptImageId: null)) return ProjectErrors.ImageKindTaken(kind);

        var nextOrder = _images.Count == 0 ? 0 : _images.Max(i => i.DisplayOrder) + 1;
        var image = new ProjectImage(imageId, Id, file, altText, label, kind, nextOrder);
        _images.Add(image);
        return image;
    }

    /// <summary>Changes a picture's alt text, caption and kind.</summary>
    /// <param name="imageId">The picture.</param>
    /// <param name="altText">Text for screen readers.</param>
    /// <param name="label">Optional caption.</param>
    /// <param name="kind">How the card uses it.</param>
    /// <returns>Success; or ImageNotFound or ImageKindTaken.</returns>
    /// <exception cref="ArgumentException">Empty alt text, or an unknown kind.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text longer than its column.</exception>
    public Result UpdateImage(Guid imageId, string altText, string? label, ProjectImageKind kind)
    {
        var image = FindImage(imageId);
        if (image is null) return ProjectErrors.ImageNotFound;
        if (IsSingleKindTaken(kind, exceptImageId: imageId)) return ProjectErrors.ImageKindTaken(kind);

        image.Change(altText, label, kind);
        return Result.Success();
    }

    /// <summary>Takes a picture off the project and closes the gap in the order.</summary>
    /// <param name="imageId">The picture.</param>
    /// <returns>The removed picture (so its files can be deleted); or ImageNotFound.</returns>
    public Result<ProjectImage> RemoveImage(Guid imageId)
    {
        var image = FindImage(imageId);
        if (image is null) return ProjectErrors.ImageNotFound;

        _images.Remove(image);
        Renumber(_images.OrderBy(i => i.DisplayOrder).ToList());
        return image;
    }

    /// <summary>Drag-to-reorder in the admin area: pass every picture Id in the new order.</summary>
    /// <param name="imageIdsInOrder">Every picture Id, each once.</param>
    /// <returns>Success; or ContentErrors.ReorderMismatch when the list doesn't match the pictures.</returns>
    public Result ReorderImages(IReadOnlyList<Guid> imageIdsInOrder)
    {
        ArgumentNullException.ThrowIfNull(imageIdsInOrder);
        if (imageIdsInOrder.Count != _images.Count || imageIdsInOrder.Distinct().Count() != _images.Count)
            return ContentErrors.ReorderMismatch;

        var ordered = new List<ProjectImage>(_images.Count);
        foreach (var id in imageIdsInOrder)
        {
            var image = FindImage(id);
            if (image is null) return ContentErrors.ReorderMismatch;
            ordered.Add(image);
        }

        Renumber(ordered);
        return Result.Success();
    }

    private ProjectImage? FindImage(Guid imageId) => _images.FirstOrDefault(i => i.Id == imageId);

    /// <summary>Primary and Secondary are one each; Gallery pictures can be many.</summary>
    private bool IsSingleKindTaken(ProjectImageKind kind, Guid? exceptImageId) =>
        kind is ProjectImageKind.Primary or ProjectImageKind.Secondary
        && _images.Any(i => i.Kind == kind && i.Id != exceptImageId);

    private static void Renumber(IReadOnlyList<ProjectImage> inOrder)
    {
        for (var i = 0; i < inOrder.Count; i++) inOrder[i].SetDisplayOrder(i);
    }
}

/// <summary>Everything about a project an admin types in, grouped so Create and Update take the same thing.</summary>
/// <param name="Title">The project's name.</param>
/// <param name="Subtitle">One line under the title.</param>
/// <param name="Description">What it is and does.</param>
/// <param name="Category">Featured or Engineering.</param>
/// <param name="Label">The small label on the card.</param>
/// <param name="VisualLabel">Optional label over the picture area.</param>
/// <param name="GithubUrl">Optional link to the source code.</param>
/// <param name="LiveUrl">Optional link to the live site.</param>
/// <param name="IsPrivateRepo">True when the code is private.</param>
/// <param name="Tags">Technologies and topics.</param>
public sealed record ProjectDetails(
    string Title,
    string Subtitle,
    string Description,
    ProjectCategory Category,
    string Label,
    string? VisualLabel,
    string? GithubUrl,
    string? LiveUrl,
    bool IsPrivateRepo,
    IReadOnlyList<string> Tags);
