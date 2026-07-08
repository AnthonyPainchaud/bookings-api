using Bookings.Application.Users.Dtos;

namespace Bookings.Application.Authentication.Dtos;

/// <summary>The result of a successful register/login: a bearer token and profile.</summary>
public record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserResponse User);
