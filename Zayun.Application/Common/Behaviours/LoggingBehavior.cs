using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behavior that logs every request's name, duration and outcome.
/// Useful for monitoring, debugging, and performance analysis.
/// </summary>
/// <remarks>
/// <para><b>Never logs the request's contents:</b> commands carry passwords, refresh tokens and reset tokens, and
/// logs are kept and shared. Only the request's NAME is logged.</para>
/// <para><b>What is logged where:</b></para>
/// <list type="bullet">
///   <item><description><b>Success:</b> Debug, with the duration.</description></item>
///   <item><description><b>Result failure</b> (wrong password, not found…): Information, with the error code. These are
///   normal outcomes, not faults.</description></item>
///   <item><description><b>Exception:</b> Debug only, with the duration, then re-thrown. The global exception handler
///   logs it once, at the right level (warning for validation, error for real faults).</description></item>
/// </list>
/// </remarks>
public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the logging behavior.
    /// </summary>
    /// <param name="logger">
    /// Logger used to record request execution information.
    /// </param>
    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes the request pipeline and logs its duration and outcome.
    /// </summary>
    /// <param name="request">
    /// The MediatR request being executed (its contents are never logged).
    /// </param>
    /// <param name="next">
    /// Delegate used to invoke the next pipeline behavior or request handler.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the request.
    /// </param>
    /// <returns>
    /// The response returned by the request handler.
    /// </returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Starting request {RequestName}", requestName);

        try
        {
            var response = await next();

            stopwatch.Stop();

            // 1. Expected failure, returned as a Result (not an exception)
            if (response is Result { IsFailure: true } failure)
            {
                _logger.LogInformation(
                    "Request {RequestName} returned {ErrorCode} after {ElapsedMilliseconds}ms",
                    requestName,
                    failure.Error.Code,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }

            // 2. Success
            _logger.LogDebug(
                "Completed request {RequestName} in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger.LogDebug(
                "Request {RequestName} was cancelled after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception)
        {
            stopwatch.Stop();

            // 3. Thrown: only the timing here; GlobalExceptionHandler logs the exception itself, once
            _logger.LogDebug(
                "Request {RequestName} threw after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}