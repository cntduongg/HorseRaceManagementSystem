using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ViolationConfiguration : IEntityTypeConfiguration<Violation>
{
    public void Configure(EntityTypeBuilder<Violation> builder)
    {
        builder.HasKey(v => v.ViolationId);

        builder.HasOne(v => v.Leg)
            .WithMany(l => l.Violations)
            .HasForeignKey(v => new { v.RaceId, v.LegNumber })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Entry)
            .WithMany()
            .HasForeignKey(v => v.EntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.ReportedByReferee)
            .WithMany()
            .HasForeignKey(v => v.ReportedByRefereeId)
            .HasConstraintName("FK_Violations_Referee")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
