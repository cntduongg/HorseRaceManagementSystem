using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
        builder.HasKey(t => t.TournamentId);
        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Status).HasMaxLength(20).IsRequired();
    }
}
