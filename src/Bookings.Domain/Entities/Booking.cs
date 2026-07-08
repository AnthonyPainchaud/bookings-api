using Bookings.Domain.Enums;

namespace Bookings.Domain.Entities;

/// <summary>
/// A reservation of a <see cref="Resource"/> by a <see cref="User"/> for a
/// half-open time interval [<see cref="StartsAt"/>, <see cref="EndsAt"/>).
/// </summary>
public class Booking
{
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties.
    public Resource? Resource { get; set; }

    public User? User { get; set; }
}
