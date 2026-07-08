using Bookings.Application.Common.Interfaces;
using Bookings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Infrastructure;

/// <summary>
/// Registers the Infrastructure layer: the EF Core context and its mapping to
/// the <see cref="IApplicationDbContext"/> abstraction consumed by Application.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' was not found. Set ConnectionStrings:Default " +
                "in configuration or the ConnectionStrings__Default environment variable.");

        services.AddDbContext<BookingsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(BookingsDbContext).Assembly.FullName)));

        // Expose the same context instance through the Application-layer abstraction.
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<BookingsDbContext>());

        return services;
    }
}
