namespace Bookings.Application.Common.Results;

/// <summary>
/// The outcome of an operation that either succeeds with a value or fails with
/// an <see cref="Results.Error"/>. Callers branch on <see cref="IsSuccess"/>.
/// </summary>
public sealed class Result<T>
{
    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Error = error;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public Error? Error { get; }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    // Lets a method simply `return Error.NotFound("...")` where a Result<T> is expected.
    public static implicit operator Result<T>(Error error) => new(error);
}
