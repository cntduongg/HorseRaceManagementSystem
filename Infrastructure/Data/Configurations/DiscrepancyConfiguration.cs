using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class DiscrepancyConfiguration : IEntityTypeConfiguration<Discrepancy>
{
    public void Configure(EntityTypeBuilder<Discrepancy> builder)
    {
        builder.HasKey(d => d.DiscrepancyId);

        builder.Property(d => d.Type)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(1000);

        builder.Property(d => d.Resolution)
            .HasMaxLength(1000);

        builder.Property(d => d.ResolutionAction)
            .HasMaxLength(30);

        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.CreatedAt);
    }
}
