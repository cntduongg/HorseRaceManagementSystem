using HorseRace.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HorseRace.DAL.Configurations;

public class JockeyInvitationConfiguration : IEntityTypeConfiguration<JockeyInvitation>
{
    public void Configure(EntityTypeBuilder<JockeyInvitation> builder)
    {
        builder.HasKey(i => i.InvitationId);

        builder.HasIndex(i => new { i.HorseOwnerId, i.JockeyId, i.HorseId, i.RaceId }).IsUnique();

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
