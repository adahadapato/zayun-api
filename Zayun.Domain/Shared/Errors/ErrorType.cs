namespace Zayun.Domain.Shared.Errors;

/// <summary>What kind of failure an <see cref="Error"/> is; Presentation turns each into an HTTP status.</summary>
public enum ErrorType
{
    Failure = 0,      // 500 / 400: something went wrong
    Validation = 1,   // 400: the input is wrong ("Price must be more than zero")
    NotFound = 2,     // 404: "Project not found"
    Conflict = 3,     // 409: "That address is already used by another project"
    Forbidden = 4,    // 403: "Only an admin can do this"
    Unauthorized = 5  // 401: not signed in, or the session has expired
}