namespace Zayun.Domain.Enums;

/// <summary>
/// What kind of role a <see cref="Entities.CareerRole"/> is, so the Experience page and the CVs can group them and put
/// each where it belongs (the work CV leads with Professional, the academic CV with Research and Teaching).
/// </summary>
/// <remarks>New values are only ever added at the end; stored numbers never change.</remarks>
public enum CareerRoleKind
{
    /// <summary>Software and systems roles, e.g. NECO, client work.</summary>
    Professional = 1,

    /// <summary>Research roles, e.g. doctoral research, research engineering.</summary>
    Research = 2,

    /// <summary>Teaching and mentoring, e.g. Graduate Teaching Assistant, coding teacher.</summary>
    Teaching = 3,

    /// <summary>Other experience, kept short, e.g. support work, invigilation.</summary>
    Other = 4
}
