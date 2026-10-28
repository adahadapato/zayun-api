using Zayun.Domain.Shared.Errors;

namespace Zayun.Domain.Shared.Results;

/// <summary>
/// Represents the result of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value, if the result is a success.
    /// Throws InvalidOperationException if the result is a failure.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    /// <summary>Returns a successful result with the specified value.</summary>
    // CHANGED: no "new": the base Success() has no parameter, so nothing is hidden (fixes CS0109).
    public static Result<T> Success(T value)
        => value is null
            ? Failure(Error.NullValue)
            : new Result<T>(value, true, Error.None);

    /// <summary>Returns a failed result with the specified error.</summary>
    // "new" is correct here: the base Failure(Error) has the same signature.
    public new static Result<T> Failure(Error error) =>
        new(default, false, error);

    /// <summary>Lets a method returning Result&lt;T&gt; simply "return value;".</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Lets a method returning Result&lt;T&gt; simply "return SomeError;".</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}