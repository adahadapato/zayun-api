using System.Reflection;
using MediatR;
using Microsoft.Extensions.Logging;
using Zayun.Application.Common.Tags;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Common.Behaviours;

/// <summary>
/// After a command marked [RefreshesTagUsage] SUCCEEDS, recounts where each tag is used. A failure here is logged
/// and never undoes the command (the counts are corrected on the next change or the next start).
/// </summary>
public sealed class TagUsageBehavior<TRequest, TResponse>(TagUsageRefresher refresher,
                                                          ILogger<TagUsageBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly bool Refreshes = typeof(TRequest).GetCustomAttribute<RefreshesTagUsageAttribute>() is not null;

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
                                        CancellationToken cancellationToken)
    {
        var response = await next();
        if (!Refreshes || response is Result { IsFailure: true }) return response;

        try
        {
            await refresher.RefreshAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Couldn't recount tag usage after {Request}.", typeof(TRequest).Name);
        }

        return response;
    }
}
