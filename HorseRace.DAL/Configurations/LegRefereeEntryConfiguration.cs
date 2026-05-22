using HorseRace.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HorseRace.DAL.Configurations;

public class LegRefereeEntryConfiguration : IEntityTypeConfiguration<LegRefereeEntry>
{
    public void Configure(EntityTypeBuilder<LegRefereeEntry> builder)
    {
        builder.HasKey(l => l.LegRefereeEntryId);

        builder.HasIndex(l => new { l.RaceId, l.LegNumber, l.EntryId, l.RefereeUserId }).IsUnique();

        builder.HasOne(l => l.Leg)
            .WithMany(leg => leg.RefereeEntries)
            .HasForeignKey(l => new { l.RaceId, l.LegNumber })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Entry)
            .WithMany(e => e.LegRefereeEntries)
            .HasForeignKey(l => l.EntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Referee)
            .WithMany()
            .HasForeignKey(l => l.RefereeUserId)
            .HasConstraintName("FK_LegRefereeEntries_Referee")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
