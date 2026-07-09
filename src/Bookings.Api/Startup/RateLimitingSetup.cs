using System.Globalization;
using Bookings.Api.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Bookings.Api.Startup;

public static class RateLimitingSetup
{
    /// <summary>Name of the stricter policy applied to the anonymous auth endpoints.</summary>
    public const string AuthPolicy = "auth";

    /// <summary>
    /// Configures request rate limiting: a global limiter partitioned by
    /// authenticated user (falling back to client IP), plus a stricter policy
    /// for the anonymous login/register endpoints where brute-forcing is a
    /// concern. Rejections are returned as ProblemDetails for a consistent
    /// error shape with the rest of the API. Permit limits are configurable
    /// (see <see cref="RateLimitingOptions"/>) so they can be tuned per
    /// environment — e.g. relaxed for the automated test suite, which
    /// legitimately registers many throwaway users in quick succession.
    /// </summary>
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var partitionKey = httpContext.User.GetUserId()?.ToString()
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.GlobalPermitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            options.AddPolicy(AuthPolicy, httpContext =>
            {
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.AuthPermitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too Many Requests",
                        Detail = "Rate limit exceeded. Please try again later."
                    }
                });
            };
        });

        return services;
    }
}
