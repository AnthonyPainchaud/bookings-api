namespace Bookings.Application.Common.Exceptions;

/// <summary>
/// Thrown when a database uniqueness/exclusion constraint is violated on save.
/// Infrastructure translates the provider-specific error (e.g. an Npgsql
/// exclusion violation) into this provider-agnostic exception so the Application
/// layer can react to a race-condition conflict without referencing Npgsql.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message, string? constraintName = null)
        : base(message)
    {
        ConstraintName = constraintName;
    }

    /// <summary>Name of the violated constraint, when the provider reports it.</summary>
    public string? ConstraintName { get; }
}
