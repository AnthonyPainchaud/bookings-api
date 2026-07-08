using Bookings.Domain.Enums;

namespace Bookings.Application.Bookings.Dtos;

public record BookingResponse(
    Guid Id,
    Guid ResourceId,
    Guid UserId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    BookingStatus Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
