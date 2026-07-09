using Bookings.Domain.Enums;

namespace Bookings.Application.Users.Dtos;

public record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role,
    DateTimeOffset CreatedAt);
