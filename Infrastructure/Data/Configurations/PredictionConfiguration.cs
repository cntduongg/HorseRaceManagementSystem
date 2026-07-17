using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PredictionConfiguration : IEntityTypeConfiguration<Prediction>
{
    public void Configure(EntityTypeBuilder<Prediction> builder)
    {
        builder.HasKey(p => p.PredictionId);

        builder.Property(p => p.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.BetAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.OddsLocked1)
            .HasPrecision(10, 4)
            .IsRequired();

        builder.Property(p => p.OddsLocked2)
            .HasPrecision(10, 4)
            .IsRequired(false);

        builder.Property(p => p.OddsLocked3)
            .HasPrecision(10, 4)
            .IsRequired(false);

        builder.HasCheckConstraint(
            "CK_Predictions_BetAmount",
            "\"BetAmount\" >= 10");

        builder.HasOne(p => p.Race)
            .WithMany(r => r.Predictions)
            .HasForeignKey(p => p.RaceId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(p => p.Leg)
            .WithMany(l => l.Predictions)
            .HasForeignKey(p => new { p.RaceId, p.LegNumber })
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(p => p.Spectator)
            .WithMany(s => s.Predictions)
            .HasForeignKey(p => p.SpectatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.FirstEntry)
            .WithMany()
            .HasForeignKey(p => p.FirstEntryId)
            .HasConstraintName("FK_Predictions_FirstEntry")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SecondEntry)
            .WithMany()
            .HasForeignKey(p => p.SecondEntryId)
            .HasConstraintName("FK_Predictions_SecondEntry")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(p => p.ThirdEntry)
            .WithMany()
            .HasForeignKey(p => p.ThirdEntryId)
            .HasConstraintName("FK_Predictions_ThirdEntry")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(p => p.RaceId);
        builder.HasIndex(p => p.SpectatorId);
        builder.HasIndex(p => p.Status);

        builder.HasIndex(p => new
        {
            p.RaceId,
            p.SpectatorId,
            p.Status
        });
    }
}