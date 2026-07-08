namespace Bookings.Infrastructure.Authentication;

/// <summary>
/// Strongly-typed JWT settings bound from the "Jwt" configuration section.
/// The signing <see cref="Key"/> is a secret and must be supplied via
/// environment/secret configuration — never committed to source.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
