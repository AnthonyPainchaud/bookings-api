using System.Text;
using Bookings.Application.Common.Interfaces;
using Bookings.Infrastructure.Authentication;
using Bookings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Infrastructure;

/// <summary>
/// Registers the Infrastructure layer: the EF Core context and its mapping to
/// the <see cref="IApplicationDbContext"/> abstraction, plus security services
/// (password hashing and JWT issuance).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Minimum signing-key length for HS256 (256 bits).</summary>
    private const int MinJwtKeyBytes = 32;

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

        AddSecurity(services, configuration);

        return services;
    }

    private static void AddSecurity(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);

        // Fail fast at startup if the JWT configuration is missing or the signing
        // key is too weak, rather than issuing unverifiable tokens at runtime.
        var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
        }

        if (Encoding.UTF8.GetByteCount(jwt.Key) < MinJwtKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:Key must be configured and at least {MinJwtKeyBytes} bytes (256 bits) for HS256. " +
                "Provide it via the Jwt__Key environment variable or another secret store.");
        }

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
    }
}
