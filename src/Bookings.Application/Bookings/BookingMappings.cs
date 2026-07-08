using Bookings.Application.Bookings.Dtos;
using Bookings.Domain.Entities;

namespace Bookings.Application.Bookings;

internal static class BookingMappings
{
    public static BookingResponse ToResponse(this Booking booking) => new(
        booking.Id,
        booking.ResourceId,
        booking.UserId,
        booking.StartsAt,
        booking.EndsAt,
        booking.Status,
        booking.Notes,
        booking.CreatedAt,
        booking.UpdatedAt);
}
