using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bookings.Application.Common.Interfaces;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bookings.Infrastructure.Authentication;

/// <summary>Issues short-lived HS256 JWT access tokens.</summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    /// <summary>
    /// Claim type carrying the user's <see cref="UserRole"/>. Short JWT-style name,
    /// consistent with the other claims here; the API's token validation is
    /// configured to treat this claim type as the role for [Authorize(Roles=...)].
    /// </summary>
    public const string RoleClaimType = "role";

    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public AccessToken GenerateToken(User user)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
            new Claim(RoleClaimType, user.Role.ToString()),
            // A unique token id enables future revocation/blacklisting if needed.
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(encoded, expiresAt);
    }
}
