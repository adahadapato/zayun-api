using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Shared.Quotes;

/// <summary>
/// A quote's web address on the website, e.g. "Nothing in biology makes sense…" by Theodosius Dobzhansky →
/// <c>theodosius-dobzhansky-nothing-in-biology-makes-sense</c> (shown at <c>/quotes/{slug}</c>).
/// </summary>
/// <remarks>
/// <para>Made once, when the quote is added, from who said it and the first few words (the English translation for
/// proverbs, so the address is readable). It never changes afterwards, even when the words are edited, so links
/// shared on LinkedIn or found on Google keep working.</para>
/// <para>Unique: when the address is already used, "-2", "-3"… is added. Addresses of deleted quotes count as used,
/// so an old link never leads to a different quote.</para>
/// </remarks>
public static class QuoteSlugs
{
    /// <summary>How many words of the quote go into the address.</summary>
    public const int WordCount = 8;

    /// <summary>Longest address (shorter than <see cref="Slugs.MaxLength"/>, to stay readable).</summary>
    public const int MaxLength = 80;

    /// <summary>Used when neither the author nor the words have any letters or digits (e.g. another script).</summary>
    public const string Fallback = "quote";

    /// <summary>Makes a new, unused address.</summary>
    /// <param name="author">Who said it.</param>
    /// <param name="text">The words, in the original language.</param>
    /// <param name="translation">The English translation, if any (preferred over <paramref name="text"/>).</param>
    /// <param name="taken">Addresses already used (any letter case is fine: slugs are lower-case).</param>
    /// <returns>The address.</returns>
    public static string Make(string author, string text, string? translation, IReadOnlySet<string> taken)
    {
        ArgumentNullException.ThrowIfNull(taken);

        var words = string.Join(' ', (string.IsNullOrWhiteSpace(translation) ? text : translation)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Take(WordCount));

        var slug = Shorten(Slugs.From($"{author} {words}"), MaxLength);
        if (slug.Length == 0) slug = Fallback;
        if (!taken.Contains(slug)) return slug;

        for (var n = 2; ; n++)
        {
            var suffix = $"-{n}";
            var candidate = Shorten(slug, MaxLength - suffix.Length) + suffix;
            if (!taken.Contains(candidate)) return candidate;
        }
    }

    /// <summary>Cuts a slug to a length at a hyphen, so no word is cut in half (unless it's one long word).</summary>
    private static string Shorten(string slug, int maxLength)
    {
        if (slug.Length <= maxLength) return slug;

        var cut = slug[..maxLength];
        var lastHyphen = cut.LastIndexOf('-');
        return lastHyphen > 0 ? cut[..lastHyphen] : cut;
    }
}
