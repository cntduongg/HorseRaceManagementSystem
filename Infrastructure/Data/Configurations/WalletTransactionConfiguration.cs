using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class WalletTransactionConfiguration
    : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasKey(t => t.WalletTransactionId);

        builder.Property(t => t.Type)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.BalanceAfter)
            .HasPrecision(18, 2);

        builder.Property(t => t.Reason)
            .HasMaxLength(500);

        builder.HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Spectator)
            .WithMany()
            .HasForeignKey(t => t.SpectatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Prediction)
            .WithMany()
            .HasForeignKey(t => t.PredictionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.SettlementRun)
            .WithMany()
            .HasForeignKey(t => t.SettlementRunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Admin)
            .WithMany()
            .HasForeignKey(t => t.AdminId)
            .HasConstraintName("FK_WalletTransactions_Admin")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.RollbackOfTransaction)
            .WithMany(t => t.RollbackTransactions)
            .HasForeignKey(t => t.RollbackOfTransactionId)
            .HasConstraintName("FK_WalletTransactions_RollbackOfTransaction")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.WalletId);
        builder.HasIndex(t => t.SpectatorId);
        builder.HasIndex(t => t.PredictionId);
        builder.HasIndex(t => t.SettlementRunId);
        builder.HasIndex(t => t.Type);
        builder.HasIndex(t => t.CreatedAt);
    }
}