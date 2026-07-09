namespace Bookings.Api.Startup;

/// <summary>Tunable request-per-minute limits, bound from the "RateLimiting" configuration section.</summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Requests per minute allowed across the whole API, per user/IP.</summary>
    public int GlobalPermitLimit { get; set; } = 100;

    /// <summary>Requests per minute allowed against the anonymous auth endpoints, per IP.</summary>
    public int AuthPermitLimit { get; set; } = 5;
}
