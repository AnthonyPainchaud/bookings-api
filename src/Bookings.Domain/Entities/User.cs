namespace Bookings.Domain.Entities;

/// <summary>
/// A person who can authenticate and make bookings.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Unique login/contact address. Enforced unique at the database level.</summary>
    public required string Email { get; set; }

    public required string FullName { get; set; }

    /// <summary>BCrypt hash of the user's password. The plaintext is never stored.</summary>
    public required string PasswordHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Bookings placed by this user.</summary>
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
