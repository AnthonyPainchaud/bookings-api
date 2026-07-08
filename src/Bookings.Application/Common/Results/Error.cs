namespace Bookings.Application.Common.Results;

/// <summary>
/// The category of an expected (non-exceptional) failure. Controllers map each
/// category onto an HTTP status code, keeping that translation in one place.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict
}

/// <summary>
/// A domain-level failure with a human-readable message. Using an explicit
/// result/error (rather than throwing) keeps expected outcomes — a double
/// booking, a missing resource — out of the exception path.
/// </summary>
public sealed record Error(ErrorType Type, string Message)
{
    public static Error Validation(string message) => new(ErrorType.Validation, message);

    public static Error NotFound(string message) => new(ErrorType.NotFound, message);

    public static Error Conflict(string message) => new(ErrorType.Conflict, message);
}
