using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class LegRefereeDraftConfiguration : IEntityTypeConfiguration<LegRefereeDraft>
{
    public void Configure(EntityTypeBuilder<LegRefereeDraft> builder)
    {
        builder.HasKey(l => l.LegRefereeDraftId);

        // 1 bản nháp cho mỗi (Race + Leg + Referee + Entry) — dùng để upsert.
        builder.HasIndex(l => new
        {
            l.RaceId,
            l.LegNumber,
            l.RefereeUserId,
            l.EntryId
        }).IsUnique();

        builder.HasOne(l => l.Leg)
            .WithMany()
            .HasForeignKey(l => new { l.RaceId, l.LegNumber })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Entry)
            .WithMany()
            .HasForeignKey(l => l.EntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Referee)
            .WithMany()
            .HasForeignKey(l => l.RefereeUserId)
            .HasConstraintName("FK_LegRefereeDrafts_Referee")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
