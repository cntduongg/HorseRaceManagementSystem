using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class HorseConfiguration : IEntityTypeConfiguration<Horse>
{
    public void Configure(EntityTypeBuilder<Horse> builder)
    {
        builder.HasKey(h => h.HorseId);

        builder.Property(h => h.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(h => h.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(h => h.Owner)
            .WithMany(u => u.OwnedHorses)
            .HasForeignKey(h => h.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(h => h.Stamina)
            .HasDefaultValue(3)
            .IsRequired();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(h => h.ApprovedBy)
            .HasConstraintName("FK_Horses_ApprovedBy")
            .OnDelete(DeleteBehavior.NoAction);
    }
}