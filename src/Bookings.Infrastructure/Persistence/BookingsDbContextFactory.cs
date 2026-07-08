using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bookings.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the EF Core tools (e.g. <c>dotnet ef migrations
/// add</c>). It lets migrations be created without booting the full API host,
/// which keeps migration generation fast and free of runtime dependencies.
///
/// The connection string here is only used to determine the provider and build
/// the model — no database connection is opened to create a migration.
/// </summary>
public class BookingsDbContextFactory : IDesignTimeDbContextFactory<BookingsDbContext>
{
    public BookingsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=bookings;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(BookingsDbContextFactory).Assembly.FullName))
            .Options;

        return new BookingsDbContext(options);
    }
}
