using Zayun.Domain.ValueObjects;

namespace Zayun.Domain.Shared.Errors;

/// <summary>
/// Represents a concrete domain error.
/// </summary>
public sealed class Error : ValueObject
{
    public Error(string code, string message, ErrorType type = ErrorType.Failure)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Type = type;
    }

    /// <summary>Gets the error code, e.g. "Project.NotFound".</summary>
    public string Code { get; }

    /// <summary>Gets the message shown to the user.</summary>
    public string Message { get; }

    /// <summary>Gets the kind of error (decides the HTTP status code).</summary>
    public ErrorType Type { get; }

    /// <summary>Gets the empty error instance (used by successful results).</summary>
    internal static Error None { get; } = new(string.Empty, string.Empty);

    /// <summary>A success result was given a null value.</summary>
    public static Error NullValue { get; } = new("Error.NullValue", "The value cannot be null.");

    // ---------- Factory helpers ----------
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>Implicitly converts the error to its code.</summary>
    public static implicit operator string(Error? error) => error?.Code ?? string.Empty;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Code;
        yield return Message;
        yield return Type;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Code}: {Message}";
}