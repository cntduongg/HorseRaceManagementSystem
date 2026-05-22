using HorseRace.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HorseRace.DAL.Configurations;

public class LegOfficialResultConfiguration : IEntityTypeConfiguration<LegOfficialResult>
{
    public void Configure(EntityTypeBuilder<LegOfficialResult> builder)
    {
        builder.HasKey(l => new { l.RaceId, l.LegNumber, l.EntryId });

        builder.HasOne(l => l.Leg)
            .WithMany(leg => leg.OfficialResults)
            .HasForeignKey(l => new { l.RaceId, l.LegNumber })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Entry)
            .WithMany(e => e.LegOfficialResults)
            .HasForeignKey(l => l.EntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
