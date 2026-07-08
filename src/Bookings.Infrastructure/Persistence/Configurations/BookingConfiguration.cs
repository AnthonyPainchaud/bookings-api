using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookings.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.StartsAt)
            .IsRequired();

        builder.Property(b => b.EndsAt)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.Notes)
            .HasMaxLength(2000);

        // Supports the common "bookings for a resource over a time range" query
        // used by availability and overlap checks.
        builder.HasIndex(b => new { b.ResourceId, b.StartsAt, b.EndsAt });

        builder.HasIndex(b => b.UserId);

        // The relationships themselves are configured from the principal side
        // (Resource/User) so the foreign-key + delete behaviour lives in one place.
    }
}
