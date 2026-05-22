using HorseRace.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HorseRace.DAL.Configurations;

public class PointWalletConfiguration : IEntityTypeConfiguration<PointWallet>
{
    public void Configure(EntityTypeBuilder<PointWallet> builder)
    {
        builder.HasKey(w => w.WalletId);

        builder.HasIndex(w => w.UserId).IsUnique();
        builder.HasCheckConstraint("CK_PointWallets_Balance", "\"Balance\" >= 0");

        builder.UseXminAsConcurrencyToken();

        builder.HasOne(w => w.User)
            .WithOne(u => u.PointWallet)
            .HasForeignKey<PointWallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
