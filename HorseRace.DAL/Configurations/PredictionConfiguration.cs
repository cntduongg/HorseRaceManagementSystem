using HorseRace.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HorseRace.DAL.Configurations;

public class PredictionConfiguration : IEntityTypeConfiguration<Prediction>
{
    public void Configure(EntityTypeBuilder<Prediction> builder)
    {
        builder.HasKey(p => p.PredictionId);

        builder.HasCheckConstraint("CK_Predictions_BetAmount", "\"BetAmount\" >= 1");

        builder.HasIndex(p => new { p.RaceId, p.UserId })
            .IsUnique()
            .HasFilter("\"Status\" = 'Pending'");

        builder.HasOne(p => p.Race)
            .WithMany(r => r.Predictions)
            .HasForeignKey(p => p.RaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.User)
            .WithMany(u => u.Predictions)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.FirstEntry)
            .WithMany()
            .HasForeignKey(p => p.FirstEntryId)
            .HasConstraintName("FK_Predictions_Entry1")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.SecondEntry)
            .WithMany()
            .HasForeignKey(p => p.SecondEntryId)
            .HasConstraintName("FK_Predictions_Entry2")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.ThirdEntry)
            .WithMany()
            .HasForeignKey(p => p.ThirdEntryId)
            .HasConstraintName("FK_Predictions_Entry3")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
