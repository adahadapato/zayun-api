namespace Zayun.Domain.Shared.Errors;

/// <summary>Every failure for private documents. Nothing here is thrown.</summary>
public static class DocumentErrors
{
    /// <summary>No document with this Id.</summary>
    public static readonly Error NotFound = Error.NotFound(
        "Document.NotFound", "That document wasn't found.");

    /// <summary>No file sent, or it was empty.</summary>
    public static readonly Error Empty = Error.Validation(
        "Document.Empty", "Please choose a file to upload.");

    /// <summary>Not a PDF, Word (.docx), JPEG or PNG file (checked from its contents, not its name).</summary>
    public static readonly Error InvalidFormat = Error.Validation(
        "Document.InvalidFormat", "Please upload a PDF, Word (.docx), JPEG or PNG file.");

    /// <summary>Over the size limit.</summary>
    public static readonly Error TooLarge = Error.Validation(
        "Document.TooLarge", "This file is too big. The maximum is 20 MB.");
}
