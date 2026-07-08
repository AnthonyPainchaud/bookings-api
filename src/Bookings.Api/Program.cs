using System.Text.Json.Serialization;
using Bookings.Api.Startup;
using Bookings.Application;
using Bookings.Infrastructure;
using Bookings.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- Service registration (composition root) ---------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        // Serialize/accept enums by name (e.g. "MeetingRoom") rather than by
        // their underlying integer, giving the API a self-describing contract.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Each layer owns its own registration extension, keeping this file declarative.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<BookingsDbContext>("database");

var app = builder.Build();

// --- HTTP request pipeline ---------------------------------------------------
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
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
