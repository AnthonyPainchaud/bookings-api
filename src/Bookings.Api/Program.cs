using System.Text.Json.Serialization;
using Asp.Versioning.ApiExplorer;
using Bookings.Api.Common;
using Bookings.Api.Startup;
using Bookings.Application;
using Bookings.Infrastructure;
using Bookings.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;

// Bootstrap logger: captures anything that happens before the full Serilog
// pipeline (built from configuration) is available, including startup failures.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Bookings API");

    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();

    // --- Service registration (composition root) -----------------------------
    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
            // Serialize/accept enums by name (e.g. "MeetingRoom") rather than by
            // their underlying integer, giving the API a self-describing contract.
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    // RFC7807 ProblemDetails for all error responses + a global handler that
    // turns unhandled exceptions into clean JSON instead of stack traces.
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddApiVersioningSetup();
    builder.Services.AddCustomRateLimiting(builder.Configuration);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
    builder.Services.AddSwaggerGen(ConfigureSwagger);

    // Each layer owns its own registration extension, keeping this file declarative.
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<BookingsDbContext>("database");

    var app = builder.Build();

    // --- HTTP request pipeline ------------------------------------------------
    // Outermost: logs one structured line per request (method, path, status,
    // elapsed) even when a downstream handler throws.
    app.UseSerilogRequestLogging();

    // Catches anything that escapes the pipeline below.
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        var versionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in versionDescriptionProvider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }
        });
    }

    // Apply any pending migrations on startup so `docker compose up` yields a
    // ready database with no manual steps. For a real production deployment
    // you'd typically run migrations as a separate, gated step instead.
    await app.ApplyMigrationsAsync();

    // Note: TLS is expected to be terminated upstream (reverse proxy / ingress),
    // so no in-process HTTPS redirection is configured here.
    app.UseAuthentication();
    app.UseAuthorization();

    // After auth so the rate limiter can partition by authenticated user id.
    app.UseRateLimiter();

    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous().DisableRateLimiting();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is deliberately thrown by WebApplicationFactory (used
    // in integration tests) to unwind Main after building the host, without
    // running it — it must propagate, not be logged as a real startup failure.
    Log.Fatal(ex, "Bookings API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Registers a JWT bearer scheme in Swagger UI so protected endpoints can be
// exercised with an "Authorize" button, and wires up XML comments + example
// request bodies for the generated documentation.
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

    // Bucket each action into the Swagger doc matching its resolved API version.
    options.DocInclusionPredicate((docName, apiDescription) =>
        apiDescription.GroupName == docName);

    foreach (var xmlFile in new[] { "Bookings.Api.xml", "Bookings.Application.xml" })
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }

    options.SchemaFilter<Bookings.Api.Startup.ExampleSchemaFilter>();
}

// Top-level statements generate an internal Program class; this partial
// re-declaration makes it public so integration tests can boot the app via
// WebApplicationFactory<Program>.
public partial class Program;
