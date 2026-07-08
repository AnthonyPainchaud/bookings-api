using Bookings.Application.Authentication;
using Bookings.Application.Bookings;
using Bookings.Application.Resources;
using Bookings.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Application;

/// <summary>
/// Registers the Application layer's services. Each layer exposes its own
/// composition-root extension so <c>Program.cs</c> stays a thin, readable
/// assembly of <c>AddXxx()</c> calls.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IAuthService, AuthService>();

        // TimeProvider makes "now" injectable, so time-dependent logic stays testable.
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
