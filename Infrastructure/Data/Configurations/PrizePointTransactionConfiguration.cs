using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PrizePointTransactionConfiguration : IEntityTypeConfiguration<PrizePointTransaction>
{
    public void Configure(EntityTypeBuilder<PrizePointTransaction> builder)
    {
        builder.HasKey(p => p.PrizePointTransactionId);

        // =====================================================
        // CORE FIELDS
        // =====================================================

        builder.Property(p => p.SourceType)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.TransactionType)
            .IsRequired();

        builder.Property(p => p.Points)
            .IsRequired();

        builder.Property(p => p.FinalPosition)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt);

        // =====================================================
        // CHECK CONSTRAINTS (SAFE RULES)
        // =====================================================

        builder.HasCheckConstraint(
            "CK_PrizePointTransactions_FinalPosition",
            "\"FinalPosition\" >= 1");

        builder.HasCheckConstraint(
            "CK_PrizePointTransactions_Points",
            "\"Points\" >= 0");

        // =====================================================
        // RELATIONSHIPS
        // =====================================================

        builder.HasOne(p => p.Tournament)
            .WithMany()
            .HasForeignKey(p => p.TournamentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Race)
            .WithMany()
            .HasForeignKey(p => p.RaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Entry)
            .WithMany()
            .HasForeignKey(p => p.EntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ⚠️ KEEP ONLY IF RaceResult is truly composite key
        builder.HasOne(p => p.RaceResult)
    .WithMany(r => r.PrizePointTransactions)
    .HasForeignKey(p => new { p.RaceId, p.EntryId })
    .HasPrincipalKey(r => new { r.RaceId, r.EntryId })
    .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.RollbackOf)
            .WithMany(p => p.Rollbacks)
            .HasForeignKey(p => p.RollbackOfId)
            .HasConstraintName("FK_PrizePointTransactions_RollbackOf")
            .OnDelete(DeleteBehavior.Restrict);

  
      

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.RaceId);
        builder.HasIndex(p => p.TournamentId);

        builder.HasIndex(p => new
        {
            p.UserId,
            p.TournamentId,
            p.RaceId
        });

        builder.HasIndex(p => new
        {
            p.UserId,
            p.TransactionType
        });

        builder.HasIndex(p => p.CreatedAt);
    }
}