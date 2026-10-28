namespace Zayun.Domain.Enums;

/// <summary>How a project picture is used on its card.</summary>
/// <remarks>
/// A card shows its Gallery pictures as a phone-screen strip when it has any; otherwise its Primary picture, with the
/// Secondary one beside it when there is one (e.g. "Public Website" and "Admin Platform").
/// </remarks>
public enum ProjectImageKind
{
    /// <summary>The main picture. At most one per project.</summary>
    Primary = 1,

    /// <summary>A second picture shown next to the main one. At most one per project.</summary>
    Secondary = 2,

    /// <summary>One screen in a gallery strip (e.g. a mobile app's screens).</summary>
    Gallery = 3
}
