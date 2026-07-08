using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookings.Infrastructure.Persistence.Configurations;

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("resources");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        // Persist the enum by name for a human-readable, refactor-safe column.
        builder.Property(r => r.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.IsActive)
            .IsRequired();

        builder.HasIndex(r => r.Name);

        builder.HasMany(r => r.Bookings)
            .WithOne(b => b.Resource!)
            .HasForeignKey(b => b.ResourceId)
            // Don't silently delete a resource's booking history — deleting a
            // resource that still has bookings is refused at the DB level.
            .OnDelete(DeleteBehavior.Restrict);
    }
}
