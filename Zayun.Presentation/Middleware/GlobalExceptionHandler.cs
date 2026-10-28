using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Zayun.Presentation.Middleware;

/// <summary>
/// Turns exceptions that escape a request into consistent RFC 7807 ProblemDetails responses.
/// </summary>
/// <remarks>
/// <para><b>Expected failures don't come here:</b> handlers return them as a <c>Result</c>, and the endpoints turn
/// those into ProblemDetails of the same shape. This handler covers what is THROWN:</para>
/// <list type="bullet">
///   <item><description><b>400</b>: validation (<c>ValidationBehavior</c>), a Domain guard (<see cref="ArgumentException"/>),
///   unreadable JSON.</description></item>
///   <item><description><b>409</b>: another request changed or created the same row at the same moment.</description></item>
///   <item><description><b>499</b>: the browser closed the connection (nothing written, logged quietly).</description></item>
///   <item><description><b>500</b>: anything else; the details stay in the log, never in the response.</description></item>
/// </list>
/// <para>Written through <see cref="IProblemDetailsService"/>, which adds the standard <c>type</c> link and a
/// <c>traceId</c> that matches the log entry.</para>
/// </remarks>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>The status nginx and others use when the client closed the request.</summary>
    private const int ClientClosedRequest = 499;

    /// <summary>SQL Server's "duplicate key" errors (unique index, unique constraint).</summary>
    private static readonly int[] DuplicateKeyErrorNumbers = [2601, 2627];

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
                                                CancellationToken cancellationToken)
    {
        // 1. The app went away: nobody is reading the answer
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request {Method} {Path} was cancelled by the client.",
                            httpContext.Request.Method, httpContext.Request.Path);
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        // 2. Exception → ProblemDetails
        var problem = ToProblem(exception);
        problem.Instance = httpContext.Request.Path;

        // 3. Log: 500s as errors (with the exception), the rest as warnings
        if (problem.Status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}.",
                            httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning("Request {Method} {Path} failed with {StatusCode}: {Message}",
                              httpContext.Request.Method, httpContext.Request.Path, problem.Status,
                              exception.Message);

        // 4. Write it
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    /// <summary>What each kind of exception means to the caller.</summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The response body (without Instance).</returns>
    private static ProblemDetails ToProblem(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(GroupByField(validation))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more fields need attention."
        },

        BadHttpRequestException badRequest => new ProblemDetails
        {
            Status = badRequest.StatusCode,
            Title = "Bad request",
            Detail = UnreadableFieldMessage(badRequest)
        },

        // A Domain guard: input that should have been caught by a validator
        ArgumentException argument and not ArgumentNullException => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request",
            Detail = WithoutParameterName(argument)
        },

        DbUpdateConcurrencyException => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Changed by someone else",
            Detail = "This was changed by another request. Please refresh and try again."
        },

        DbUpdateException { InnerException: SqlException sql } when DuplicateKeyErrorNumbers.Contains(sql.Number) =>
            new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Already exists",
                Detail = "Something with the same details was just created. Please refresh and try again."
            },

        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Something went wrong",
            Detail = "An unexpected error occurred. Please try again later."
        }
    };

    /// <summary>FluentValidation's failures as { "Email": ["…", "…"], "Password": ["…"] }.</summary>
    private static Dictionary<string, string[]> GroupByField(ValidationException validation) =>
        validation.Errors
                  .GroupBy(error => error.PropertyName)
                  .ToDictionary(group => group.Key,
                                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

    /// <summary>The guard's message without .NET's " (Parameter 'email')" suffix.</summary>
    private static string WithoutParameterName(ArgumentException argument) =>
        argument.ParamName is null
            ? argument.Message
            : argument.Message.Replace($" (Parameter '{argument.ParamName}')", string.Empty);

    /// <summary>
    /// "The value for 'country' isn't valid." when the JSON names the field; otherwise a general message.
    /// </summary>
    private static string UnreadableFieldMessage(BadHttpRequestException badRequest)
    {
        if (badRequest.InnerException is System.Text.Json.JsonException { Path: { Length: > 2 } path })
            return $"The value for '{path.TrimStart('$', '.')}' isn't valid. Please check it and try again.";

        return "The request couldn't be read. Please check the data sent.";
    }
}