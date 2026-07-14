using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class PasswordResetOtpConfiguration
    : IEntityTypeConfiguration<PasswordResetOtp>
{
    public void Configure(
        EntityTypeBuilder<PasswordResetOtp> builder)
    {
        builder.HasKey(o => o.OtpId);

        builder.Property(o => o.OtpCodeHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(o => o.FailedAttempts)
            .HasDefaultValue(0)
            .IsConcurrencyToken();

        builder.Property(o => o.UsedAt)
            .IsConcurrencyToken();

        builder.HasIndex(o => new
            {
                o.UserId,
                o.CreatedAt
            })
            .HasDatabaseName(
                "IX_PasswordResetOtps_Active_UserId_CreatedAt")
            .HasFilter("\"UsedAt\" IS NULL");

        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}