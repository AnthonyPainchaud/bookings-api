using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookings.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        // BCrypt hashes are 60 characters; leave headroom for algorithm changes.
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(100);

        // One account per email address.
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasMany(u => u.Bookings)
            .WithOne(b => b.User!)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
