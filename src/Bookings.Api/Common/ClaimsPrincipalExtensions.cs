using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Bookings.Api.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the authenticated user's id from the token's <c>sub</c> claim.
    /// Returns <c>null</c> when absent or malformed.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var id) ? id : null;
    }
}
