using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class SpectatorConfiguration : IEntityTypeConfiguration<Spectator>
{
    public void Configure(EntityTypeBuilder<Spectator> builder)
    {
        builder.HasKey(s => s.UserId);

        builder.Property(s => s.RegisteredAt)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(s => s.User)
            .WithOne()
            .HasForeignKey<Spectator>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}