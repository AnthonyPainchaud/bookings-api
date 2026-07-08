using Bookings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bookings.Api.Startup;

/// <summary>
/// Startup helpers for bringing the database schema up to date.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Applies any pending EF Core migrations, retrying briefly so the app can
    /// tolerate the database container still finishing its start-up.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        const int maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(3);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BookingsDbContext>>();

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
                return;
            }
            catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException && attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Database not ready (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}s...",
                    attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }
    }
}
