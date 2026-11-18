using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// One role in the owner's career, e.g. "Assistant Director (Programme Analyst), National Examinations Council (NECO),
/// 2006 – 2024": shown on the Experience page under "Roles" and used by the work and academic CVs.
/// </summary>
/// <remarks>
/// <para>Called <c>CareerRole</c> in the code so it isn't mixed up with the sign-in system's roles (admin
/// permissions); the website calls them "Roles".</para>
/// <para><b>Dates:</b> a start year, optionally a month; no end year means the role is current ("Present"). The
/// website orders roles by these dates (current first, then newest), so <see cref="ContentEntity.DisplayOrder"/> isn't
/// used for them.</para>
/// <para>Bad input is caught by the validators first, so the checks here THROW.</para>
/// </remarks>
public class CareerRole : ContentEntity
{
    /// <summary>Longest job title.</summary>
    public const int TitleMaxLength = 150;

    /// <summary>Longest organisation name.</summary>
    public const int OrganisationMaxLength = 150;

    /// <summary>Longest place, e.g. "London, United Kingdom" or "Remote".</summary>
    public const int LocationMaxLength = 100;

    /// <summary>Longest one-line summary.</summary>
    public const int SummaryMaxLength = 500;

    /// <summary>Longest achievement bullet.</summary>
    public const int HighlightMaxLength = 300;

    /// <summary>Most achievement bullets.</summary>
    public const int MaxHighlights = 6;

    /// <summary>Earliest year accepted.</summary>
    public const int MinYear = 1950;

    /// <summary>Latest year accepted.</summary>
    public const int MaxYear = 2100;

    /// <summary>Required by EF Core.</summary>
    protected CareerRole() { }

    private CareerRole(Guid id, int displayOrder, bool isPublished) : base(id, displayOrder, isPublished) { }

    /// <summary>Professional, research, teaching or other.</summary>
    public CareerRoleKind Kind { get; private set; }

    /// <summary>The job title, e.g. "Graduate Teaching Assistant".</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Where, e.g. "Brunel University London".</summary>
    public string Organisation { get; private set; } = string.Empty;

    /// <summary>The place, e.g. "London, United Kingdom" or "Remote"; null when not shown.</summary>
    public string? Location { get; private set; }

    /// <summary>The year it started.</summary>
    public int StartYear { get; private set; }

    /// <summary>The month it started (1–12); null when only the year is shown.</summary>
    public int? StartMonth { get; private set; }

    /// <summary>The year it ended; null while it's current.</summary>
    public int? EndYear { get; private set; }

    /// <summary>The month it ended (1–12); null when only the year is shown, or while it's current.</summary>
    public int? EndMonth { get; private set; }

    /// <summary>One line about the role (optional), e.g. "Doctoral-level research in Medical AI; degree not awarded."</summary>
    public string? Summary { get; private set; }

    /// <summary>Achievements and responsibilities, one per bullet, in the order shown.</summary>
    public List<string> Highlights { get; private set; } = [];

    /// <summary>True while the role is current (no end year).</summary>
    public bool IsCurrent => EndYear is null;

    /// <summary>Adds a role.</summary>
    /// <param name="details">Its details.</param>
    /// <param name="displayOrder">Its place in the admin list.</param>
    /// <param name="isPublished">Shown straight away, or kept as a draft.</param>
    /// <returns>The new role.</returns>
    /// <exception cref="ArgumentException">Missing text, or an unknown kind.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, a bad year or month, or an end before the start.</exception>
    public static CareerRole Create(CareerRoleDetails details, int displayOrder, bool isPublished)
    {
        var role = new CareerRole(Guid.CreateVersion7(), displayOrder, isPublished);
        role.Update(details);
        return role;
    }

    /// <summary>Changes every detail.</summary>
    /// <param name="details">The new details.</param>
    /// <exception cref="ArgumentException">Missing text, or an unknown kind.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, a bad year or month, or an end before the start.</exception>
    public void Update(CareerRoleDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        if (!Enum.IsDefined(details.Kind)) throw new ArgumentException("Unknown kind of role.", nameof(details));

        CheckYear(details.StartYear, nameof(details.StartYear));
        CheckMonth(details.StartMonth, nameof(details.StartMonth));
        if (details.EndYear is { } endYear) CheckYear(endYear, nameof(details.EndYear));
        CheckMonth(details.EndMonth, nameof(details.EndMonth));
        if (details.EndYear is null && details.EndMonth is not null)
            throw new ArgumentOutOfRangeException(nameof(details), "An end month needs an end year.");
        if (EndsBeforeStart(details))
            throw new ArgumentOutOfRangeException(nameof(details), "The role can't end before it starts.");

        Kind = details.Kind;
        Title = TextRules.Required(details.Title, TitleMaxLength, nameof(details.Title));
        Organisation = TextRules.Required(details.Organisation, OrganisationMaxLength, nameof(details.Organisation));
        Location = TextRules.Optional(details.Location, LocationMaxLength, nameof(details.Location));
        StartYear = details.StartYear;
        StartMonth = details.StartMonth;
        EndYear = details.EndYear;
        EndMonth = details.EndMonth;
        Summary = TextRules.Optional(details.Summary, SummaryMaxLength, nameof(details.Summary));
        Highlights = TextRules.Labels(details.Highlights, HighlightMaxLength, MaxHighlights, nameof(details.Highlights));
    }

    /// <summary>True when the end is before the start (months count only when both are given).</summary>
    /// <param name="d">The details.</param>
    public static bool EndsBeforeStart(CareerRoleDetails d) =>
        d.EndYear is { } end &&
        (end < d.StartYear || (end == d.StartYear && d.StartMonth is { } sm && d.EndMonth is { } em && em < sm));

    private static void CheckYear(int year, string paramName)
    {
        if (year is < MinYear or > MaxYear)
            throw new ArgumentOutOfRangeException(paramName, $"The year must be between {MinYear} and {MaxYear}.");
    }

    private static void CheckMonth(int? month, string paramName)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(paramName, "The month must be between 1 and 12.");
    }
}

/// <summary>Everything about a role that can be edited.</summary>
/// <param name="Kind">Professional, research, teaching or other.</param>
/// <param name="Title">The job title.</param>
/// <param name="Organisation">Where.</param>
/// <param name="Location">The place (optional).</param>
/// <param name="StartYear">The year it started.</param>
/// <param name="StartMonth">The month it started (optional, 1–12).</param>
/// <param name="EndYear">The year it ended; null while current.</param>
/// <param name="EndMonth">The month it ended (optional, 1–12; needs an end year).</param>
/// <param name="Summary">One line about the role (optional).</param>
/// <param name="Highlights">Achievement bullets (optional; empty ones are dropped).</param>
public sealed record CareerRoleDetails(
    CareerRoleKind Kind,
    string Title,
    string Organisation,
    string? Location,
    int StartYear,
    int? StartMonth,
    int? EndYear,
    int? EndMonth,
    string? Summary,
    IEnumerable<string>? Highlights);
