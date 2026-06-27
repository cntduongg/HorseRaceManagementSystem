using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShareKernel.UnitOfWork;

namespace Infrastructure.Data;

using Application.Common.Interfaces;

public class ApplicationDbContext
    : DbContext,
      IApplicationDbContext,
      IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Spectator> Spectators => Set<Spectator>();
    public DbSet<JockeyProfile> JockeyProfiles => Set<JockeyProfile>();

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

    public DbSet<SettlementRun> SettlementRuns => Set<SettlementRun>();
    public DbSet<PredictionSettlement> PredictionSettlements => Set<PredictionSettlement>();
    public DbSet<PrizePointTransaction> PrizePointTransactions => Set<PrizePointTransaction>();
    public DbSet<ReviewHistory> ReviewHistories => Set<ReviewHistory>();
    public DbSet<Discrepancy> Discrepancies => Set<Discrepancy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();

        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            return _currentTransaction;
        }

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);

        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    private void ApplyAuditValues()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added &&
                entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
            {
                entry.Property("CreatedAt").CurrentValue = now;
            }

            if (entry.State == EntityState.Modified &&
                entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}