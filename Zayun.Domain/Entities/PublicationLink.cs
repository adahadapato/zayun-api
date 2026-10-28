using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// One "read it here" link on a publication, e.g. "View on IEEE Xplore". Part of the publication (stored with it),
/// with no identity of its own: the whole list is replaced when the publication is edited.
/// </summary>
public sealed class PublicationLink
{
    /// <summary>Longest button text.</summary>
    public const int LabelMaxLength = 60;

    /// <summary>Longest address.</summary>
    public const int UrlMaxLength = 500;

    /// <summary>Required by EF Core.</summary>
    private PublicationLink() { }

    private PublicationLink(string label, string url, bool isPrimary)
    {
        Label = label;
        Url = url;
        IsPrimary = isPrimary;
    }

    /// <summary>Button text.</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>An absolute http(s) address.</summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>Shown as the main (filled) button.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Makes a link.</summary>
    /// <param name="label">Button text.</param>
    /// <param name="url">An absolute http(s) address.</param>
    /// <param name="isPrimary">Shown as the main button.</param>
    /// <returns>The link.</returns>
    /// <exception cref="ArgumentException">Empty text, or not a web address.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long.</exception>
    public static PublicationLink Create(string label, string url, bool isPrimary) =>
        new(TextRules.Required(label, LabelMaxLength, nameof(label)),
            TextRules.RequiredUrl(url, UrlMaxLength, nameof(url)),
            isPrimary);
}
