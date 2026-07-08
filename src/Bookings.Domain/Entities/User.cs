namespace Bookings.Domain.Entities;

/// <summary>
/// A person who can make bookings. Authentication/identity concerns are out of
/// scope for now — this is purely the domain record of who owns a reservation.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Unique login/contact address. Enforced unique at the database level.</summary>
    public required string Email { get; set; }

    public required string FullName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Bookings placed by this user.</summary>
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
