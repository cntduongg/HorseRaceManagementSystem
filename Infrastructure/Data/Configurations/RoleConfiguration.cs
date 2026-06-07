using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.RoleId);

        builder.Property(r => r.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(r => r.Code)
            .IsUnique();

        builder.HasData(
            new Role { RoleId = 1, Code = "HORSE_OWNER", Name = "Horse Owner" },
            new Role { RoleId = 2, Code = "JOCKEY", Name = "Jockey" },
            new Role { RoleId = 3, Code = "REFEREE", Name = "Race Referee" },
            new Role { RoleId = 4, Code = "SPECTATOR", Name = "Spectator" },
            new Role { RoleId = 5, Code = "ADMIN", Name = "Administrator" }
        );
    }
}