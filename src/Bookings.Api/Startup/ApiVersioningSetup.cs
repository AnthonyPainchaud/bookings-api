using Asp.Versioning;

namespace Bookings.Api.Startup;

public static class ApiVersioningSetup
{
    /// <summary>
    /// Configures URL-segment API versioning (<c>/api/v1/...</c>). Requests with
    /// no version specified default to 1.0, and the version shows up in
    /// responses via the <c>api-supported-versions</c> header.
    /// </summary>
    public static IServiceCollection AddApiVersioningSetup(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                // Formats the version as "v1" for both route substitution and
                // the Swagger document group name.
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
