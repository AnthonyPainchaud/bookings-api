using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace Bookings.IntegrationTests;

/// <summary>
/// Boots the real API (via <see cref="WebApplicationFactory{TEntryPoint}"/>)
/// against a disposable PostgreSQL container. The app's own startup path
/// (<c>Program.cs</c>) applies migrations automatically on first request, so
/// no manual migration step is needed here — tests exercise exactly the same
/// startup behavior as production.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("bookings_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Infrastructure's JWT setup validates Jwt:Key synchronously at DI
        // registration time (line 48 of Program.cs), which runs *before*
        // WebApplicationBuilder.Build() — the point at which a test factory's
        // ConfigureWebHost/ConfigureAppConfiguration overrides normally take
        // effect for minimal-hosting apps. That's too late here, so
        // configuration is supplied via process environment variables instead:
        // WebApplication.CreateBuilder(args) reads those immediately as part
        // of its own construction, before any later line in Program.cs runs.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Issuer", "bookings-api-tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "bookings-api-tests-clients");
        Environment.SetEnvironmentVariable("Jwt__Key", "integration-test-signing-key-at-least-32-bytes-long");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");

        // The suite legitimately registers many throwaway users in quick
        // succession across tests; the production auth rate limit (5/min per
        // IP) would otherwise make the run flaky. Rate limiting itself stays
        // wired up and exercised — only the permit counts are relaxed.
        Environment.SetEnvironmentVariable("RateLimiting__GlobalPermitLimit", "100000");
        Environment.SetEnvironmentVariable("RateLimiting__AuthPermitLimit", "100000");
    }

    async Task IAsyncLifetime.DisposeAsync() => await _postgres.DisposeAsync();
}

/// <summary>
/// Shares one <see cref="CustomWebApplicationFactory"/> (and its PostgreSQL
/// container) across every integration test class, so the container starts
/// once per test run rather than once per class.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Integration";
}
