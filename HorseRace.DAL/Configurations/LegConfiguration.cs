using HorseRace.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HorseRace.DAL.Configurations;

public class LegConfiguration : IEntityTypeConfiguration<Leg>
{
    public void Configure(EntityTypeBuilder<Leg> builder)
    {
        builder.HasKey(l => new { l.RaceId, l.LegNumber });

        builder.HasCheckConstraint("CK_Legs_LegNumber",
            "\"LegNumber\" >= 1 AND \"LegNumber\" <= 10");

        builder.HasOne(l => l.Race)
            .WithMany(r => r.Legs)
            .HasForeignKey(l => l.RaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
