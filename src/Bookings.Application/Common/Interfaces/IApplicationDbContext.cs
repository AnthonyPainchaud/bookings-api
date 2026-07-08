using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the persistence context that the Application layer depends
/// on. Infrastructure provides the concrete EF Core implementation. Depending on
/// this interface (rather than the concrete DbContext) keeps services decoupled
/// from the database provider and straightforward to test.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Resource> Resources { get; }

    DbSet<Booking> Bookings { get; }

    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
