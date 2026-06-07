using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class JockeyProfileConfiguration : IEntityTypeConfiguration<JockeyProfile>
{
    public void Configure(EntityTypeBuilder<JockeyProfile> builder)
    {
        builder.HasKey(j => j.UserId);

        builder.Property(j => j.LicenseNumber)
            .HasMaxLength(100);

        builder.Property(j => j.Weight)
            .HasPrecision(5, 2);

        builder.Property(j => j.Bio)
            .HasMaxLength(1000);

        builder.Property(j => j.TotalRaces)
            .HasDefaultValue(0);

        builder.Property(j => j.TotalWins)
            .HasDefaultValue(0);

        builder.Property(j => j.TotalTop3)
            .HasDefaultValue(0);

        builder.Property(j => j.CareerPrizePoints)
            .HasDefaultValue(0);

        builder.HasOne(j => j.User)
            .WithOne()
            .HasForeignKey<JockeyProfile>(j => j.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}