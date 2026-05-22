using HorseRace.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HorseRace.DAL.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.LicenseNumber)
            .IsUnique()
            .HasFilter("\"LicenseNumber\" IS NOT NULL");

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(new User
        {
            UserId       = 1,
            Email        = "admin@horserace.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FullName     = "System Admin",
            RoleId       = 5,
            IsActive     = true,
            CreatedAt    = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
