using Bookings.Domain.Enums;

namespace Bookings.Application.Bookings.Dtos;

/// <summary>
/// A booking enriched with its resource and owner details, for the admin
/// all-bookings view. Denormalized (vs. plain <see cref="BookingResponse"/>) so
/// the admin UI doesn't need extra round-trips to resolve names.
/// </summary>
public record AdminBookingResponse(
    Guid Id,
    Guid ResourceId,
    string ResourceName,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    BookingStatus Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
