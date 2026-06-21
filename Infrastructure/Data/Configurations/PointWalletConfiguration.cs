using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PointWalletConfiguration : IEntityTypeConfiguration<PointWallet>
{
    public void Configure(EntityTypeBuilder<PointWallet> builder)
    {
        builder.HasKey(w => w.WalletId);

        builder.HasIndex(w => w.SpectatorId)
            .IsUnique();

        builder.Property(w => w.Balance)
            .HasPrecision(18, 2)
            .HasDefaultValue(100);

        builder.Property(w => w.IsFrozen)
            .HasDefaultValue(false);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_PointWallets_Balance",
                "\"Balance\" >= 0");
        });

        builder.HasOne(w => w.Spectator)
            .WithOne(s => s.PointWallet)
            .HasForeignKey<PointWallet>(w => w.SpectatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}