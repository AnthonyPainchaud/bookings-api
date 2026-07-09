using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bookings.Api.Startup;

/// <summary>
/// Generates one Swagger document per discovered API version, so
/// <c>/swagger/v1/swagger.json</c> only lists v1 endpoints (and a future v2
/// document would only list v2 endpoints).
/// </summary>
public class ConfigureSwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "Bookings API",
                Version = description.ApiVersion.ToString(),
                Description = "A REST API for managing bookable resources and their reservations."
                    + (description.IsDeprecated ? " This API version is deprecated." : string.Empty)
            });
        }
    }

    public void Configure(string? name, SwaggerGenOptions options) => Configure(options);
}
