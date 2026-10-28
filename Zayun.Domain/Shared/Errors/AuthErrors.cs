namespace Zayun.Domain.Shared.Errors;

/// <summary>Sign-in and account failures returned by IAuthService, and written by Presentation when a request is
/// refused.</summary>
public static class AuthErrors
{
    // ---------- Sign-in ----------

    /// <summary>Deliberately vague: never reveal whether the email exists.</summary>
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Auth.InvalidCredentials", "The email or password is incorrect.");

    /// <summary>Too many wrong passwords in a row.</summary>
    public static readonly Error LockedOut = Error.Forbidden(
        "Auth.LockedOut", "Too many failed attempts. Please try again in a few minutes.");

    /// <summary>The account isn't an admin (Zayun has no other kind of sign-in).</summary>
    public static readonly Error NotAnAdmin = Error.Forbidden(
        "Auth.NotAnAdmin", "This account can't sign in to the admin area.");

    /// <summary>The refresh token is unknown, revoked or expired.</summary>
    public static readonly Error InvalidToken = Error.Unauthorized(
        "Auth.InvalidToken", "Your session has expired. Please sign in again.");

    // ---------- Account ----------

    /// <summary>Changing the password: the current one is wrong.</summary>
    public static readonly Error WrongPassword = Error.Validation(
        "Auth.WrongPassword", "Your current password isn't right.");

    /// <summary>The new password breaks the password rules.</summary>
    /// <param name="reasons">Identity's descriptions, joined.</param>
    /// <returns>The error.</returns>
    public static Error WeakPassword(string reasons) => Error.Validation(
        "Auth.WeakPassword", reasons);

    /// <summary>The signed-in account no longer exists.</summary>
    public static readonly Error UserNotFound = Error.NotFound(
        "Auth.UserNotFound", "That account wasn't found.");

    // ---------- Access (written by Presentation when a request is refused) ----------

    /// <summary>No access token sent.</summary>
    public static readonly Error SignInRequired = Error.Unauthorized(
        "Auth.SignInRequired", "Please sign in to do this.");

    /// <summary>The access token has expired: refresh it, or sign in again.</summary>
    public static readonly Error SessionExpired = Error.Unauthorized(
        "Auth.SessionExpired", "Your session has expired. Please sign in again.");

    /// <summary>The access token is damaged, forged or for another app.</summary>
    public static readonly Error InvalidAccessToken = Error.Unauthorized(
        "Auth.InvalidAccessToken", "Your sign-in isn't valid. Please sign in again.");

    /// <summary>Signed in, but this needs a role the user doesn't have.</summary>
    public static readonly Error NotAllowed = Error.Forbidden(
        "Auth.NotAllowed", "You don't have permission to do this.");
}
