using Zayun.Domain.Shared.Errors;

namespace Zayun.Domain.Shared.Results;

/// <summary>
/// Represents the result of an operation, with success/failure status and optional error.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A success result cannot have an error.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failure result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the result is a success.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the result is a failure.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the error, if any.</summary>
    public Error Error { get; }

    /// <summary>Creates a success result (no value).</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Returns a failed result with the specified error.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Creates a success result with a value.</summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>Creates a failed result for an operation that would have returned a value.</summary>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>Returns the first failure from the results, or success if all succeed.</summary>
    public static Result FirstFailureOrSuccess(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
                return result;
        }
        return Success();
    }

    /// <summary>Lets a method returning Result simply "return SomeError;".</summary>
    public static implicit operator Result(Error error) => Failure(error);
}