using HorseRace.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HorseRace.DAL.Configurations;

public class RaceResultConfiguration : IEntityTypeConfiguration<RaceResult>
{
    public void Configure(EntityTypeBuilder<RaceResult> builder)
    {
        builder.HasKey(r => new { r.RaceId, r.EntryId });

        builder.HasOne(r => r.Race)
            .WithMany(race => race.RaceResults)
            .HasForeignKey(r => r.RaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Entry)
            .WithOne(e => e.RaceResult)
            .HasForeignKey<RaceResult>(r => r.EntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
