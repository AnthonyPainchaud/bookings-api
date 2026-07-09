using Bookings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bookings.UnitTests.TestSupport;

/// <summary>
/// Builds a <see cref="BookingsDbContext"/> backed by a uniquely-named EF Core
/// InMemory database, so each test gets an isolated database with zero setup.
/// </summary>
public static class InMemoryDbContextFactory
{
    public static BookingsDbContext Create()
    {
        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BookingsDbContext(options);
    }
}
