using System.Reflection;
using Bookings.Application.Common.Exceptions;
using Bookings.Application.Common.Interfaces;
using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    /// <summary>
    /// Translates PostgreSQL uniqueness/exclusion-constraint violations into a
    /// provider-agnostic <see cref="ConflictException"/>. This keeps Npgsql
    /// details out of the Application layer while still letting services convert
    /// a race-condition conflict into a clean 409 response.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
            pg.SqlState is PostgresErrorCodes.ExclusionViolation or PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException(pg.MessageText, pg.ConstraintName);
        }
    }
}
