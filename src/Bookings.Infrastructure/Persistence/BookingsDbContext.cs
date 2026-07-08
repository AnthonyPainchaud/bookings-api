using System.Reflection;
using Bookings.Application.Common.Interfaces;
using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the bookings domain. Implements
/// <see cref="IApplicationDbContext"/> so the Application layer can depend on the
/// abstraction rather than this concrete type.
/// </summary>
public class BookingsDbContext : DbContext, IApplicationDbContext
{
    public BookingsDbContext(DbContextOptions<BookingsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Pick up every IEntityTypeConfiguration<T> in this assembly, keeping the
        // context lean and each entity's mapping in its own focused file.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
