using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class RaceResultConfiguration : IEntityTypeConfiguration<RaceResult>
{
    public void Configure(EntityTypeBuilder<RaceResult> builder)
    {
        builder.HasKey(r => new { r.RaceId, r.EntryId });


        builder.HasIndex(r => new { r.RaceId, r.EntryId })
               .IsUnique();

        builder.HasOne(r => r.Race)
            .WithMany(race => race.RaceResults)
            .HasForeignKey(r => r.RaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Entry)
    .WithMany(e => e.RaceResults)
    .HasForeignKey(r => r.EntryId)
    .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.TotalPoints)
     .HasPrecision(8, 2)
     .HasDefaultValue(0m);
        builder.Property(r => r.LegWinCount).HasDefaultValue(0);
        builder.Property(r => r.LegTop3Count).HasDefaultValue(0);
        builder.Property(r => r.IsRaceDQ).HasDefaultValue(false);
        builder.Property(x => x.IsDisqualified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ViolationNote)
            .HasMaxLength(500);
    }
}