using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class RaceConfiguration : IEntityTypeConfiguration<Race>
{
    public void Configure(EntityTypeBuilder<Race> builder)
    {
        builder.HasKey(r => r.RaceId);

        builder.HasCheckConstraint("CK_Races_NumberOfLegs",
            "\"NumberOfLegs\" >= 1 AND \"NumberOfLegs\" <= 10");
        builder.HasCheckConstraint("CK_Races_DifferentReferees",
            "\"Referee1Id\" <> \"Referee2Id\"");

        builder.HasOne(r => r.Referee1)
            .WithMany()
            .HasForeignKey(r => r.Referee1Id)
            .HasConstraintName("FK_Races_Referee1")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.Referee2)
            .WithMany()
            .HasForeignKey(r => r.Referee2Id)
            .HasConstraintName("FK_Races_Referee2")
            .OnDelete(DeleteBehavior.NoAction);

        // Auto-start worker: quét Status=Scheduled + ScheduledStartTime <= now
        builder.HasIndex(r => new { r.Status, r.ScheduledStartTime })
            .HasDatabaseName("IX_Races_Status_ScheduledStartTime");
    }
}
