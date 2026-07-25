using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();

        builder.Property(u => u.Status)
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("Active");

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.LicenseNumber)
            .IsUnique()
            .HasFilter("\"LicenseNumber\" IS NOT NULL");

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(user => user.NormalizedPhoneNumber)
            .HasMaxLength(20);

        builder.HasIndex(user => user.NormalizedPhoneNumber)
            .HasDatabaseName(
                "UX_Users_NormalizedPhoneNumber")
            .IsUnique()
            .HasFilter(
                "\"NormalizedPhoneNumber\" IS NOT NULL");

        builder.HasData(new User
        {
            UserId       = 1,
            Email        = "admin@horserace.com",
            PasswordHash = "$2a$11$cHcPtVLvCvKHR/30b2HuQOMs7GqrfTEAr5FcjLqCQzf9zCECIqqmW",
            FullName = "System Admin",
            RoleId       = 5,
            IsActive     = true,
            Status       = "Active",
            CreatedAt    = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
