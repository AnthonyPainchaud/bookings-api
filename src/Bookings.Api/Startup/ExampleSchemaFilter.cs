using Bookings.Application.Authentication.Dtos;
using Bookings.Application.Bookings.Dtos;
using Bookings.Application.Resources.Dtos;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bookings.Api.Startup;

/// <summary>
/// Attaches realistic example values to the request DTOs' Swagger schemas, so
/// "Try it out" in Swagger UI starts from a working payload instead of an
/// empty/zeroed one.
/// </summary>
public class ExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        schema.Example = context.Type switch
        {
            Type t when t == typeof(RegisterRequest) => new OpenApiObject
            {
                ["email"] = new OpenApiString("alice@example.com"),
                ["fullName"] = new OpenApiString("Alice Anderson"),
                ["password"] = new OpenApiString("S3curePassw0rd")
            },
            Type t when t == typeof(LoginRequest) => new OpenApiObject
            {
                ["email"] = new OpenApiString("alice@example.com"),
                ["password"] = new OpenApiString("S3curePassw0rd")
            },
            Type t when t == typeof(CreateResourceRequest) => new OpenApiObject
            {
                ["name"] = new OpenApiString("Conference Room A"),
                ["description"] = new OpenApiString("10-seat room with projector"),
                ["type"] = new OpenApiString("MeetingRoom"),
                ["capacity"] = new OpenApiInteger(10)
            },
            Type t when t == typeof(UpdateResourceRequest) => new OpenApiObject
            {
                ["name"] = new OpenApiString("Conference Room A"),
                ["description"] = new OpenApiString("10-seat room with projector"),
                ["type"] = new OpenApiString("MeetingRoom"),
                ["capacity"] = new OpenApiInteger(10),
                ["isActive"] = new OpenApiBoolean(true)
            },
            Type t when t == typeof(CreateBookingRequest) => new OpenApiObject
            {
                ["resourceId"] = new OpenApiString("00000000-0000-0000-0000-000000000000"),
                ["startsAt"] = new OpenApiString(DateTime.UtcNow.AddDays(1).ToString("O")),
                ["endsAt"] = new OpenApiString(DateTime.UtcNow.AddDays(1).AddHours(1).ToString("O")),
                ["notes"] = new OpenApiString("Sprint planning")
            },
            _ => schema.Example
        };
    }
}
