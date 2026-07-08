using Bookings.Domain.Entities;

namespace Bookings.Application.Common.Interfaces;

/// <summary>An issued access token and the instant it expires.</summary>
public record AccessToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// Issues signed JWT access tokens for authenticated users. Implemented in
/// Infrastructure so token/crypto concerns stay out of the Application layer.
/// </summary>
public interface IJwtTokenGenerator
{
    AccessToken GenerateToken(User user);
}
