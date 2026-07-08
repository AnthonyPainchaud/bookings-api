namespace Bookings.Domain.Enums;

/// <summary>
/// Lifecycle state of a <see cref="Entities.Booking"/>.
/// </summary>
public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2
}
