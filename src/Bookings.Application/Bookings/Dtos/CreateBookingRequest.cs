using System.ComponentModel.DataAnnotations;

namespace Bookings.Application.Bookings.Dtos;

/// <summary>
/// Payload for creating a booking. The owner is taken from the authenticated
/// caller's token — never from the request body — so a client cannot book on
/// behalf of another user. Temporal rules (end after start, not in the past,
/// max duration) depend on the current time and are enforced in the service.
/// </summary>
public record CreateBookingRequest(
    Guid ResourceId,

    DateTimeOffset StartsAt,

    DateTimeOffset EndsAt,

    [StringLength(2000)]
    string? Notes);
