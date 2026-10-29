using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Text;
using Zayun.Domain.ValueObjects;

namespace Zayun.Domain.Entities;

/// <summary>
/// One of the owner's private documents (CV, certificates, transcripts…): never shown on the website, only listed
/// in the admin area and opened through a short-lived signed link.
/// </summary>
/// <remarks>
/// <para>Bad text is caught by the validators first, so the checks here THROW.</para>
/// <para>Deleting a document removes the row AND its file (the handler deletes the file): private documents
/// shouldn't linger after the owner asked for them to go.</para>
/// </remarks>
public class Document : Entity<Guid>
{
    /// <summary>Longest title.</summary>
    public const int TitleMaxLength = 150;

    /// <summary>Longest note.</summary>
    public const int NotesMaxLength = 1000;

    /// <summary>Required by EF Core.</summary>
    protected Document() { }

    private Document(Guid id) : base(id) { }

    /// <summary>The name shown in the list, e.g. "CV — October 2026".</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>CV, certificate, transcript…</summary>
    public DocumentCategory Category { get; private set; }

    /// <summary>A private note, e.g. "Sent to Brunel HR".</summary>
    public string? Notes { get; private set; }

    /// <summary>The stored file.</summary>
    public DocumentFile File { get; private set; } = null!;

    /// <summary>Records an uploaded document.</summary>
    /// <param name="id">The Id it was stored under (also used in its path).</param>
    /// <param name="title">The name shown in the list.</param>
    /// <param name="category">What kind it is.</param>
    /// <param name="notes">Optional private note.</param>
    /// <param name="file">The stored file.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentException">An empty title or an unknown category.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long.</exception>
    public static Document Create(Guid id, string title, DocumentCategory category, string? notes, DocumentFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        var document = new Document(id) { File = file };
        document.Update(title, category, notes);
        return document;
    }

    /// <summary>Changes the title, category and note.</summary>
    /// <exception cref="ArgumentException">An empty title or an unknown category.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long.</exception>
    public void Update(string title, DocumentCategory category, string? notes)
    {
        if (!Enum.IsDefined(category)) throw new ArgumentException("Unknown document category.", nameof(category));
        Title = TextRules.Required(title, TitleMaxLength, nameof(title));
        Category = category;
        Notes = TextRules.Optional(notes, NotesMaxLength, nameof(notes));
    }

    /// <summary>Swaps in a new version of the file (e.g. an updated CV).</summary>
    /// <param name="file">The new file.</param>
    /// <returns>The old file, so it can be deleted.</returns>
    public DocumentFile ReplaceFile(DocumentFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        var previous = File;
        File = file;
        return previous;
    }
}
