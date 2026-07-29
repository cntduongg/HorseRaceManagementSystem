using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class LegOfficialResultConfiguration : IEntityTypeConfiguration<LegOfficialResult>
{
    public void Configure(EntityTypeBuilder<LegOfficialResult> builder)
    {
        builder.HasKey(l => new
        {
            l.RaceId,
            l.LegNumber,
            l.EntryId
        });

        builder.Property(l => l.LegPoints)
            .HasPrecision(6, 2); // đủ chứa fieldSize lớn × 1.1, VD 99.9

        builder.Property(l => l.ResultStatus)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.ConfirmationType)
            .HasMaxLength(30)
            .IsRequired();

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