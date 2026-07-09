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
    /// error shape with the rest of the API.
    /// </summary>
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
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
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            options.AddPolicy(AuthPolicy, httpContext =>
            {
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
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
