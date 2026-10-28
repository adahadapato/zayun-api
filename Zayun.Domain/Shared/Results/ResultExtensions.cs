using Zayun.Domain.Shared.Errors;

namespace Zayun.Domain.Shared.Results;

/// <summary>
/// Contains extension methods for the result class.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Ensures the predicate is true for a successful result, otherwise returns a failure with the error.
    /// </summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error)
    {
        if (result.IsFailure)
            return result;

        // CHANGED: no second IsSuccess check; it's already known to be a success here.
        return predicate(result.Value) ? result : Result.Failure<T>(error);
    }

    /// <summary>Maps the value to a new value if successful; otherwise passes the failure on.</summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> func)
        => result.IsSuccess
            ? Result<TOut>.Success(func(result.Value))
            : Result<TOut>.Failure(result.Error);

    // ---------- Bind ----------

    /// <summary>NEW: runs the next step only if this one succeeded (synchronous).</summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> func)
        => result.IsSuccess ? func(result.Value) : Result.Failure<TOut>(result.Error);

    /// <summary>Runs the next async step only if this one succeeded.</summary>
    public static async Task<Result> Bind<TIn>(this Result<TIn> result, Func<TIn, Task<Result>> func)
        => result.IsSuccess ? await func(result.Value) : Result.Failure(result.Error);

    /// <summary>Runs the next async step only if this one succeeded.</summary>
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> func)
        => result.IsSuccess ? await func(result.Value) : Result.Failure<TOut>(result.Error);

    // ---------- Match ----------

    /// <summary>NEW: picks onSuccess or onFailure (synchronous, e.g. in a controller).</summary>
    public static TOut Match<TOut>(this Result result, Func<TOut> onSuccess, Func<Error, TOut> onFailure)
        => result.IsSuccess ? onSuccess() : onFailure(result.Error);

    /// <summary>NEW: picks onSuccess(value) or onFailure (synchronous).</summary>
    public static TOut Match<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> onSuccess, Func<Error, TOut> onFailure)
        => result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);

    /// <summary>Awaits the result, then picks onSuccess or onFailure.</summary>
    public static async Task<T> Match<T>(this Task<Result> resultTask, Func<T> onSuccess, Func<Error, T> onFailure)
    {
        var result = await resultTask;
        return result.IsSuccess ? onSuccess() : onFailure(result.Error);
    }

    /// <summary>Awaits the result, then picks onSuccess(value) or onFailure.</summary>
    public static async Task<TOut> Match<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, TOut> onSuccess,
        Func<Error, TOut> onFailure)
    {
        var result = await resultTask;
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
    }
}