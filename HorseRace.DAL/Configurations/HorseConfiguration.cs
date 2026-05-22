using HorseRace.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HorseRace.DAL.Configurations;

public class HorseConfiguration : IEntityTypeConfiguration<Horse>
{
    public void Configure(EntityTypeBuilder<Horse> builder)
    {
        builder.HasKey(h => h.HorseId);
        builder.Property(h => h.Status).HasMaxLength(20).IsRequired();

        builder.HasOne(h => h.Owner)
            .WithMany(u => u.OwnedHorses)
            .HasForeignKey(h => h.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(h => h.ApprovedBy)
            .HasConstraintName("FK_Horses_ApprovedBy")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
