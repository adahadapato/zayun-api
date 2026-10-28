namespace Zayun.Domain.Interfaces;

/// <summary>
/// Who is making the current request.
/// Handlers use this instead of taking a userId from the request body,
/// so nobody can act as someone else by sending a different Id.
///
/// Implemented in Presentation (reads the JWT claims from HttpContext).
/// </summary>
public interface ICurrentUser
{
    /// <summary>The signed-in user's Id; null if not signed in.</summary>
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    /// <summary>Check a role, e.g. IsInRole(Roles.Admin).</summary>
    bool IsInRole(string role);
}