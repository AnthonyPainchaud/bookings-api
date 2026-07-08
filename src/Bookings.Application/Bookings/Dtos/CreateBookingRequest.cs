using System.ComponentModel.DataAnnotations;

namespace Bookings.Application.Bookings.Dtos;

/// <summary>
/// Payload for creating a booking. Structural validation is handled here;
/// temporal rules (end after start, not in the past, max duration) depend on the
/// current time and are enforced in the service.
/// </summary>
public record CreateBookingRequest(
    Guid ResourceId,

    Guid UserId,

    DateTimeOffset StartsAt,

    DateTimeOffset EndsAt,

    [StringLength(2000)]
    string? Notes);
