using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class JockeyInvitationConfiguration : IEntityTypeConfiguration<JockeyInvitation>
{
    public void Configure(EntityTypeBuilder<JockeyInvitation> builder)
    {
        builder.HasKey(i => i.InvitationId);

        builder.Property(i => i.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(i => new
        {
            i.HorseOwnerId,
            i.JockeyId,
            i.HorseId,
            i.RaceId
        }).IsUnique();

        builder.HasOne(i => i.HorseOwner)
            .WithMany(u => u.SentInvitations)
            .HasForeignKey(i => i.HorseOwnerId)
            .HasConstraintName("FK_JockeyInvitations_Owner")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Jockey)
            .WithMany(u => u.ReceivedInvitations)
            .HasForeignKey(i => i.JockeyId)
            .HasConstraintName("FK_JockeyInvitations_Jockey")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Horse)
            .WithMany(h => h.Invitations)
            .HasForeignKey(i => i.HorseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Race)
            .WithMany()
            .HasForeignKey(i => i.RaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}