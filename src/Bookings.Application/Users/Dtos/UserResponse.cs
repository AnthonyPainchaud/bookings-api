namespace Bookings.Application.Users.Dtos;

public record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    DateTimeOffset CreatedAt);
