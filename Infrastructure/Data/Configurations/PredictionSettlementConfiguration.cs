using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PredictionSettlementConfiguration : IEntityTypeConfiguration<PredictionSettlement>
{
    public void Configure(EntityTypeBuilder<PredictionSettlement> builder)
    {
        builder.HasKey(p => p.PredictionSettlementId);

        builder.Property(p => p.Outcome)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.BetAmount)
            .HasPrecision(18, 2);

        builder.Property(p => p.OddsAverage)
            .HasPrecision(10, 4);

        builder.Property(p => p.PayoutAmount)
            .HasPrecision(18, 2);

        builder.Property(p => p.NetAmount)
            .HasPrecision(18, 2);

        builder.Property(p => p.IsRollbacked)
            .HasDefaultValue(false);

        builder.HasCheckConstraint(
            "CK_PredictionSettlements_MatchedCount",
            "\"MatchedCount\" >= 0 AND \"MatchedCount\" <= 3");

        builder.HasOne(p => p.SettlementRun)
            .WithMany(s => s.PredictionSettlements)
            .HasForeignKey(p => p.SettlementRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Prediction)
            .WithMany()
            .HasForeignKey(p => p.PredictionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Race)
            .WithMany()
            .HasForeignKey(p => p.RaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Spectator)
            .WithMany(s => s.PredictionSettlements)
            .HasForeignKey(p => p.SpectatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PayoutTransaction)
            .WithMany()
            .HasForeignKey(p => p.PayoutTransactionId)
            .HasConstraintName("FK_PredictionSettlements_PayoutTransaction")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.RollbackOfSettlement)
            .WithMany(p => p.RollbackSettlements)
            .HasForeignKey(p => p.RollbackOfSettlementId)
            .HasConstraintName("FK_PredictionSettlements_RollbackOfSettlement")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.SettlementRunId);
        builder.HasIndex(p => p.PredictionId);
        builder.HasIndex(p => p.RaceId);
        builder.HasIndex(p => p.SpectatorId);
        builder.HasIndex(p => p.Outcome);

        builder.HasIndex(p => new
        {
            p.SettlementRunId,
            p.PredictionId
        }).IsUnique();
    }
}