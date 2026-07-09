using Serilog;
using Serilog.Formatting.Compact;

namespace Bookings.Api.Startup;

public static class LoggingSetup
{
    /// <summary>
    /// Replaces the default logging provider with Serilog. Log levels and
    /// overrides are configuration-driven (see the "Serilog" section in
    /// appsettings.json); the output format is chosen here per environment —
    /// human-readable in Development, compact JSON (for log aggregation)
    /// otherwise.
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId();

            if (context.HostingEnvironment.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
            }
            else
            {
                loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
            }
        });

        return builder;
    }
}
