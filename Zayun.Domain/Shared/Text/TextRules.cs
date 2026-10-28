namespace Zayun.Domain.Shared.Text;

/// <summary>
/// The checks every entity makes on the text it's given. Validators check the same rules first and give friendly
/// messages, so these THROW: reaching one means a bug, not a typing mistake.
/// </summary>
public static class TextRules
{
    /// <summary>Required text: trimmed, not empty, not too long.</summary>
    /// <param name="value">The text.</param>
    /// <param name="maxLength">Longest allowed (after trimming).</param>
    /// <param name="paramName">For the exception.</param>
    /// <returns>The trimmed text.</returns>
    /// <exception cref="ArgumentException">Empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Too long.</exception>
    public static string Required(string? value, int maxLength, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        var clean = value.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(clean.Length, maxLength, paramName);
        return clean;
    }

    /// <summary>Optional text: trimmed, or null when empty.</summary>
    /// <param name="value">The text.</param>
    /// <param name="maxLength">Longest allowed (after trimming).</param>
    /// <param name="paramName">For the exception.</param>
    /// <returns>The trimmed text, or null.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Too long.</exception>
    public static string? Optional(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var clean = value.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(clean.Length, maxLength, paramName);
        return clean;
    }

    /// <summary>Optional web link: an absolute http(s) address, or null when empty.</summary>
    /// <param name="value">The link.</param>
    /// <param name="maxLength">Longest allowed.</param>
    /// <param name="paramName">For the exception.</param>
    /// <returns>The trimmed link, or null.</returns>
    /// <exception cref="ArgumentException">Not an absolute http or https address.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Too long.</exception>
    public static string? OptionalUrl(string? value, int maxLength, string paramName)
    {
        var clean = Optional(value, maxLength, paramName);
        if (clean is null) return null;
        if (!IsWebUrl(clean))
            throw new ArgumentException("Must be an absolute http or https address.", paramName);
        return clean;
    }

    /// <summary>Required web link.</summary>
    /// <param name="value">The link.</param>
    /// <param name="maxLength">Longest allowed.</param>
    /// <param name="paramName">For the exception.</param>
    /// <returns>The trimmed link.</returns>
    /// <exception cref="ArgumentException">Empty, or not an absolute http or https address.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Too long.</exception>
    public static string RequiredUrl(string? value, int maxLength, string paramName)
    {
        var clean = Required(value, maxLength, paramName);
        if (!IsWebUrl(clean))
            throw new ArgumentException("Must be an absolute http or https address.", paramName);
        return clean;
    }

    /// <summary>A list of short labels (tags, skills): trimmed, blanks dropped, duplicates removed (ignoring case),
    /// in the order given.</summary>
    /// <param name="values">The labels.</param>
    /// <param name="maxItemLength">Longest label.</param>
    /// <param name="maxCount">Most labels.</param>
    /// <param name="paramName">For the exception.</param>
    /// <returns>The clean list.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A label too long, or too many labels.</exception>
    public static List<string> Labels(IEnumerable<string>? values, int maxItemLength, int maxCount, string paramName)
    {
        var clean = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values ?? [])
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            var label = value.Trim();
            ArgumentOutOfRangeException.ThrowIfGreaterThan(label.Length, maxItemLength, paramName);
            if (seen.Add(label)) clean.Add(label);
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(clean.Count, maxCount, paramName);
        return clean;
    }

    /// <summary>True for an absolute http or https address.</summary>
    /// <param name="value">The text to check.</param>
    public static bool IsWebUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
