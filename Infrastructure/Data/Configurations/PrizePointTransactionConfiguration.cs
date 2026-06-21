using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PrizePointTransactionConfiguration : IEntityTypeConfiguration<PrizePointTransaction>
{
    public void Configure(EntityTypeBuilder<PrizePointTransaction> builder)
    {
        builder.HasKey(p => p.PrizePointTransactionId);

        builder.Property(p => p.EntityType)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.Type)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasCheckConstraint(
            "CK_PrizePointTransactions_FinalPosition",
            "\"FinalPosition\" >= 1");

        builder.HasCheckConstraint(
            "CK_PrizePointTransactions_Points",
            "\"Points\" >= 0");

        builder.HasOne(p => p.RaceResult)
            .WithMany()
            .HasForeignKey(p => new { p.RaceId, p.EntryId })
            .OnDelete(DeleteBehavior.Restrict);

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

        builder.HasOne(p => p.RollbackOf)
    .WithMany(p => p.Rollbacks)
    .HasForeignKey(p => p.RollbackOfId)
    .HasConstraintName("FK_PrizePointTransactions_RollbackOf")
    .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.TournamentId);
        builder.HasIndex(p => p.RaceId);
        builder.HasIndex(p => p.EntryId);
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.EntityType);
        builder.HasIndex(p => p.Type);

        builder.HasIndex(p => new
        {
            p.RaceId,
            p.EntryId,
            p.UserId,
            p.Type
        });
    }
}