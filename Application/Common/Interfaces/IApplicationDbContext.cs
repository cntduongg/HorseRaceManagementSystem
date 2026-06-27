using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Role> Roles { get; }

    DbSet<User> Users { get; }

    DbSet<Entry> Entries { get; }

    DbSet<Horse> Horses { get; }

    DbSet<Race> Races { get; }

    DbSet<Tournament> Tournaments { get; }

    DbSet<Leg> Legs { get; }

    DbSet<JockeyInvitation> JockeyInvitations { get; }

    DbSet<LegRefereeEntry> LegRefereeEntries { get; }

    DbSet<LegOfficialResult> LegOfficialResults { get; }

    DbSet<RaceResult> RaceResults { get; }

    DbSet<Violation> Violations { get; }

    DbSet<PointWallet> PointWallets { get; }

    DbSet<WalletTransaction> WalletTransactions { get; }

    DbSet<Prediction> Predictions { get; }

    DbSet<SettlementRun> SettlementRuns { get; }

    DbSet<PredictionSettlement> PredictionSettlements { get; }

    DbSet<PrizePointTransaction> PrizePointTransactions { get; }

    DbSet<Spectator> Spectators { get; }

    DbSet<JockeyProfile> JockeyProfiles { get; }

    DbSet<PasswordResetOtp> PasswordResetOtps { get; }
    DbSet<ReviewHistory> ReviewHistories { get; }
    DbSet<Discrepancy> Discrepancies { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
    DatabaseFacade Database { get; }
}