using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Presentation.Extensions;

/// <summary>
/// Turns a handler's <see cref="Result"/> into an HTTP response, so endpoints stay one line.
/// </summary>
/// <remarks>
/// <para><b>Success:</b> 200 with the value, or 204 when there's nothing to return.</para>
/// <para><b>Failure:</b> ProblemDetails, the same shape as <c>GlobalExceptionHandler</c> (with <c>traceId</c>), plus a
/// <c>code</c> field (e.g. "Auth.InvalidCredentials") that the website can switch on. Also used for 401 / 403 written by
/// authentication.</para>
/// </remarks>
internal static class ResultExtensions
{
    /// <summary>204 No Content, or the error.</summary>
    /// <param name="result">The handler's result.</param>
    public static IResult ToHttp(this Result result) =>
        result.IsSuccess ? Results.NoContent() : result.Error.ToProblem();

    /// <summary>200 OK with the value, or the error.</summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="result">The handler's result.</param>
    public static IResult ToHttp<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();

    /// <summary>An error as ProblemDetails with the matching status code.</summary>
    /// <param name="error">The error.</param>
    public static IResult ToProblem(this Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Invalid request"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Not signed in"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Not allowed"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Something went wrong")
        };

        return Results.Problem(
            statusCode: status,
            title: title,
            detail: error.Message,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}