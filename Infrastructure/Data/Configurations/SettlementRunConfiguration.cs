using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class SettlementRunConfiguration : IEntityTypeConfiguration<SettlementRun>
{
    public void Configure(EntityTypeBuilder<SettlementRun> builder)
    {
        builder.HasKey(s => s.SettlementRunId);

        builder.Property(s => s.Type)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.TotalBetAmount)
            .HasPrecision(18, 2);

        builder.Property(s => s.TotalPayoutAmount)
            .HasPrecision(18, 2);

        builder.HasOne(s => s.Race)
            .WithMany()
            .HasForeignKey(s => s.RaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.TriggeredByAdmin)
            .WithMany()
            .HasForeignKey(s => s.TriggeredByAdminId)
            .HasConstraintName("FK_SettlementRuns_TriggeredByAdmin")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.PredictionSettlements)
            .WithOne(ps => ps.SettlementRun)
            .HasForeignKey(ps => ps.SettlementRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.RaceId);
        builder.HasIndex(s => s.Type);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.CreatedAt);
    }
}