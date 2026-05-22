using HorseRace.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace HorseRace.DAL.Context;

public class HorseRaceDbContext : DbContext
{
    public HorseRaceDbContext(DbContextOptions<HorseRaceDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<Horse> Horses => Set<Horse>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Race> Races => Set<Race>();
    public DbSet<Leg> Legs => Set<Leg>();
    public DbSet<JockeyInvitation> JockeyInvitations => Set<JockeyInvitation>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<LegRefereeEntry> LegRefereeEntries => Set<LegRefereeEntry>();
    public DbSet<LegOfficialResult> LegOfficialResults => Set<LegOfficialResult>();
    public DbSet<Violation> Violations => Set<Violation>();
    public DbSet<RaceResult> RaceResults => Set<RaceResult>();
    public DbSet<PointWallet> PointWallets => Set<PointWallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<Prediction> Predictions => Set<Prediction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HorseRaceDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        foreach (var e in ChangeTracker.Entries())
        {
            if (e.State == EntityState.Added)
                if (e.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
                    e.Property("CreatedAt").CurrentValue = now;
            if (e.State == EntityState.Modified)
                if (e.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                    e.Property("UpdatedAt").CurrentValue = now;
        }
        return base.SaveChangesAsync(ct);
    }
}
