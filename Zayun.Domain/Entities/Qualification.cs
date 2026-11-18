using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// One qualification under "Education" on the Experience page and the CVs, e.g. "M.Tech, Computer Science, Federal
/// University of Technology, Minna, 2023 (Distinction, CGPA 4.48/5.00)".
/// </summary>
/// <remarks>
/// <para><b>Order:</b> by <see cref="Year"/> (in progress first, then newest), so
/// <see cref="ContentEntity.DisplayOrder"/> isn't used for qualifications.</para>
/// <para>Bad input is caught by the validators first, so the checks here THROW.</para>
/// </remarks>
public class Qualification : ContentEntity
{
    /// <summary>Longest degree, e.g. "B.Sc." or "Diploma".</summary>
    public const int DegreeMaxLength = 100;

    /// <summary>Longest subject.</summary>
    public const int SubjectMaxLength = 150;

    /// <summary>Longest institution name.</summary>
    public const int InstitutionMaxLength = 150;

    /// <summary>Longest place, e.g. "Minna, Nigeria".</summary>
    public const int LocationMaxLength = 100;

    /// <summary>Longest grade or note.</summary>
    public const int NoteMaxLength = 200;

    /// <summary>Earliest year accepted.</summary>
    public const int MinYear = 1950;

    /// <summary>Latest year accepted.</summary>
    public const int MaxYear = 2100;

    /// <summary>Required by EF Core.</summary>
    protected Qualification() { }

    private Qualification(Guid id, int displayOrder, bool isPublished) : base(id, displayOrder, isPublished) { }

    /// <summary>The degree or award, e.g. "M.Tech".</summary>
    public string Degree { get; private set; } = string.Empty;

    /// <summary>The subject, e.g. "Computer Science".</summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>Where, e.g. "University of Uyo".</summary>
    public string Institution { get; private set; } = string.Empty;

    /// <summary>The place, e.g. "Uyo, Nigeria"; null when not shown.</summary>
    public string? Location { get; private set; }

    /// <summary>The year it was completed; null while in progress.</summary>
    public int? Year { get; private set; }

    /// <summary>A grade or note, e.g. "Distinction, CGPA 4.48/5.00"; null for none.</summary>
    public string? Note { get; private set; }

    /// <summary>True while it's in progress (no year).</summary>
    public bool IsInProgress => Year is null;

    /// <summary>Adds a qualification.</summary>
    /// <param name="details">Its details.</param>
    /// <param name="displayOrder">Its place in the admin list.</param>
    /// <param name="isPublished">Shown straight away, or kept as a draft.</param>
    /// <returns>The new qualification.</returns>
    /// <exception cref="ArgumentException">Missing text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or a bad year.</exception>
    public static Qualification Create(QualificationDetails details, int displayOrder, bool isPublished)
    {
        var qualification = new Qualification(Guid.CreateVersion7(), displayOrder, isPublished);
        qualification.Update(details);
        return qualification;
    }

    /// <summary>Changes every detail.</summary>
    /// <param name="details">The new details.</param>
    /// <exception cref="ArgumentException">Missing text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or a bad year.</exception>
    public void Update(QualificationDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        if (details.Year is < MinYear or > MaxYear)
            throw new ArgumentOutOfRangeException(nameof(details), $"The year must be between {MinYear} and {MaxYear}.");

        Degree = TextRules.Required(details.Degree, DegreeMaxLength, nameof(details.Degree));
        Subject = TextRules.Required(details.Subject, SubjectMaxLength, nameof(details.Subject));
        Institution = TextRules.Required(details.Institution, InstitutionMaxLength, nameof(details.Institution));
        Location = TextRules.Optional(details.Location, LocationMaxLength, nameof(details.Location));
        Year = details.Year;
        Note = TextRules.Optional(details.Note, NoteMaxLength, nameof(details.Note));
    }
}

/// <summary>Everything about a qualification that can be edited.</summary>
/// <param name="Degree">The degree or award.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Institution">Where.</param>
/// <param name="Location">The place (optional).</param>
/// <param name="Year">The year completed; null while in progress.</param>
/// <param name="Note">A grade or note (optional).</param>
public sealed record QualificationDetails(
    string Degree,
    string Subject,
    string Institution,
    string? Location,
    int? Year,
    string? Note);
