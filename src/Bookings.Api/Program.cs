using System.Text.Json.Serialization;
using Bookings.Api.Common;
using Bookings.Api.Startup;
using Bookings.Application;
using Bookings.Infrastructure;
using Bookings.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Service registration (composition root) ---------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        // Serialize/accept enums by name (e.g. "MeetingRoom") rather than by
        // their underlying integer, giving the API a self-describing contract.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// RFC7807 ProblemDetails for all error responses + a global handler that turns
// unhandled exceptions into clean JSON instead of stack traces.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(ConfigureSwagger);

// Each layer owns its own registration extension, keeping this file declarative.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<BookingsDbContext>("database");

var app = builder.Build();

// --- HTTP request pipeline ---------------------------------------------------
// First in the pipeline so it catches exceptions from everything downstream.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply any pending migrations on startup so `docker compose up` yields a ready
// database with no manual steps. For a real production deployment you'd typically
// run migrations as a separate, gated step rather than on app boot.
await app.ApplyMigrationsAsync();

// Note: TLS is expected to be terminated upstream (reverse proxy / ingress), so
// no in-process HTTPS redirection is configured here.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Registers a JWT bearer scheme in Swagger UI so protected endpoints can be
// exercised with an "Authorize" button.
static void ConfigureSwagger(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
{
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token (without the 'Bearer ' prefix).",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
}
