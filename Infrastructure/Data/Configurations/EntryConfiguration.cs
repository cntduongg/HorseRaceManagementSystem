using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder.HasKey(e => e.EntryId);

        builder.Property(e => e.Odds).HasPrecision(10, 4);

        builder.HasIndex(e => new { e.RaceId, e.HorseId }).IsUnique();
        builder.HasIndex(e => new { e.RaceId, e.JockeyId }).IsUnique();
        builder.HasIndex(e => new { e.RaceId, e.GateNumber })
            .IsUnique()
            .HasFilter("\"GateNumber\" IS NOT NULL");

        builder.HasOne(e => e.Race)
            .WithMany(r => r.Entries)
            .HasForeignKey(e => e.RaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Horse)
            .WithMany(h => h.Entries)
            .HasForeignKey(e => e.HorseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Jockey)
            .WithMany()
            .HasForeignKey(e => e.JockeyId)
            .HasConstraintName("FK_Entries_Jockey")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.HorseOwner)
            .WithMany()
            .HasForeignKey(e => e.HorseOwnerId)
            .HasConstraintName("FK_Entries_Owner")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ApprovedBy)
            .HasConstraintName("FK_Entries_ApprovedBy")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
