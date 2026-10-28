using System.Globalization;
using System.Text;

namespace Zayun.Domain.Shared.Text;

/// <summary>
/// Web addresses for content, e.g. "RCCG Hope House" → "rccg-hope-house" (used as <c>/projects/rccg-hope-house</c>).
/// </summary>
public static class Slugs
{
    /// <summary>Longest slug (column size).</summary>
    public const int MaxLength = 160;

    /// <summary>Makes a slug from any text: lower-case letters and digits joined by single hyphens.</summary>
    /// <param name="text">A title, or a slug typed by the admin.</param>
    /// <returns>The slug; empty when the text has no letters or digits at all.</returns>
    /// <remarks>Accents are removed ("Café" → "cafe") and symbols dropped ("GuidelineIQ™" → "guidelineiq").</remarks>
    public static string From(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var decomposed = text.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingHyphen = false;

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;   // accents

            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                AppendLetter(c);
            }
            else if (c is >= 'A' and <= 'Z')
            {
                AppendLetter(char.ToLowerInvariant(c));
            }
            else if (char.IsWhiteSpace(c) || c is '-' or '_' or '.' or '/' or '&' or '+')
            {
                pendingHyphen = builder.Length > 0;
            }
            // anything else (™, ©, punctuation, other scripts) is dropped
        }

        var slug = builder.ToString();
        return slug.Length <= MaxLength ? slug : slug[..MaxLength].TrimEnd('-');

        void AppendLetter(char letter)
        {
            if (pendingHyphen) builder.Append('-');
            pendingHyphen = false;
            builder.Append(letter);
        }
    }

    /// <summary>True when the text is already a clean slug (what <see cref="From"/> would return for it).</summary>
    /// <param name="slug">The text to check.</param>
    public static bool IsValid(string? slug) =>
        !string.IsNullOrEmpty(slug) && slug.Length <= MaxLength && From(slug) == slug;
}
