using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Domain.Aggregates.Constants;
namespace Application.Usecases.RaceExecution;

public enum RaceStartOutcome
{
    Started,
    AlreadyStarted,
    Skipped
}

public sealed record CloseRegistrationLifecycleResult(
    int RaceId,
    int ApprovedEntries,
    int RejectedEntries);

public sealed record RaceStartLifecycleResult(
    int RaceId,
    RaceStartOutcome Outcome,
    string? Status,
    int? TotalLegs,
    string? SkipReason,
    bool RegistrationWasClosed);

/// <summary>
/// Điều phối đóng đăng ký + start Race trong một transaction (idempotent).
/// Dùng chung bởi endpoint thủ công và worker auto-start.
/// </summary>
public interface IRaceLifecycleCoordinator
{
    Task<CloseRegistrationLifecycleResult> CloseRegistrationAsync(
        int raceId,
        CancellationToken cancellationToken = default);

    /// <param name="enforceSchedule">true = từ chối nếu chưa tới ScheduledStartTime.</param>
    /// <param name="allowAutoClose">true = tự đóng đăng ký nếu chưa đóng.</param>
    /// <param name="throwOnFailure">true = ném exception (HTTP thủ công); false = trả Skipped (worker).</param>
    Task<RaceStartLifecycleResult> StartRaceAsync(
        int raceId,
        bool enforceSchedule,
        bool allowAutoClose,
        bool throwOnFailure,
        CancellationToken cancellationToken = default);
}

public sealed class RaceLifecycleCoordinator : IRaceLifecycleCoordinator
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly IRaceLiveChangeTracker _liveTracker;

    public RaceLifecycleCoordinator(
        IApplicationDbContext context,
        TimeProvider timeProvider,
        IRaceLiveChangeTracker liveTracker)
    {
        _context = context;
        _timeProvider = timeProvider;
        _liveTracker = liveTracker;
    }

    public async Task<CloseRegistrationLifecycleResult> CloseRegistrationAsync(
        int raceId,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await LockRaceRowAsync(raceId, cancellationToken);

            var race = await _context.Races
                .FirstOrDefaultAsync(r => r.RaceId == raceId, cancellationToken)
                ?? throw new KeyNotFoundException("Race not found.");
            if (race.Status == RaceStatus.Cancelled)
            {
                throw new InvalidOperationException(
                    "Cancelled races cannot close registration.");
            }
            if (race.Status != RaceExecutionConstants.RaceScheduled)
                throw new InvalidOperationException(
                    $"Registration can only be closed while the race is Scheduled (current: {race.Status}).");
            if (race.OddsComputedAt != null)
                throw new InvalidOperationException("Registration has already been closed.");

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var (approved, rejected) = await ApplyCloseRegistrationAsync(race, now, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new CloseRegistrationLifecycleResult(race.RaceId, approved, rejected);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RaceStartLifecycleResult> StartRaceAsync(
        int raceId,
        bool enforceSchedule,
        bool allowAutoClose,
        bool throwOnFailure,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await LockRaceRowAsync(raceId, cancellationToken);

            var race = await _context.Races
                .Include(r => r.Legs)
                .Include(r => r.Entries)
                .FirstOrDefaultAsync(r => r.RaceId == raceId, cancellationToken)
                ?? throw new KeyNotFoundException("Race not found.");

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if (race.Status == RaceExecutionConstants.RaceInProgress)
            {
                if (throwOnFailure)
                    throw new InvalidOperationException(
                        $"The race can only be started in Scheduled status (current: {race.Status}).");

                await tx.CommitAsync(cancellationToken);
                return new RaceStartLifecycleResult(
                    race.RaceId,
                    RaceStartOutcome.AlreadyStarted,
                    race.Status,
                    race.NumberOfLegs,
                    null,
                    RegistrationWasClosed: false);
            }

            if (race.Status != RaceExecutionConstants.RaceScheduled)
            {
                return await FailOrSkipAsync(
                    tx,
                    race.RaceId,
                    throwOnFailure,
                    $"The race can only be started in Scheduled status (current: {race.Status}).",
                    cancellationToken);
            }

            if (enforceSchedule && race.ScheduledStartTime > now)
            {
                return await FailOrSkipAsync(
                    tx,
                    race.RaceId,
                    throwOnFailure,
                    "The race cannot be started before its ScheduledStartTime. It will start automatically at the scheduled time.",
                    cancellationToken);
            }

            if (race.Referee1Id is null || race.Referee2Id is null)
            {
                return await FailOrSkipAsync(
                    tx,
                    race.RaceId,
                    throwOnFailure,
                    "The race has not been assigned 2 referees yet.",
                    cancellationToken);
            }

            var registrationWasClosed = false;
            if (race.OddsComputedAt is null)
            {
                if (!allowAutoClose)
                {
                    return await FailOrSkipAsync(
                        tx,
                        race.RaceId,
                        throwOnFailure,
                        "Registration must be closed (Odds locked) before starting the race.",
                        cancellationToken);
                }

                var approvedIfClosed = race.Entries
                    .Count(e => e.Status == RaceExecutionConstants.EntryApproved);
                if (approvedIfClosed < 2)
                {
                    return await FailOrSkipAsync(
                        tx,
                        race.RaceId,
                        throwOnFailure,
                        "At least 2 approved entries are required to close registration and start the race.",
                        cancellationToken);
                }

                await ApplyCloseRegistrationAsync(race, now, cancellationToken);
                registrationWasClosed = true;
            }
            else
            {
                var approvedEntries = race.Entries
                    .Count(e => e.Status == RaceExecutionConstants.EntryApproved);
                if (approvedEntries < 2)
                {
                    return await FailOrSkipAsync(
                        tx,
                        race.RaceId,
                        throwOnFailure,
                        "At least 2 approved entries are required to start the race.",
                        cancellationToken);
                }
            }

            await RaceLegProvisioner.EnsureLegsExistAsync(_context, race, cancellationToken);

            race.Status = RaceExecutionConstants.RaceInProgress;
            race.UpdatedAt = now;

            // Per-leg betting lock: StartLegCommand locks Pending predictions for that leg only.
            // Do not lock all race predictions here — legs not yet started must stay Pending (T-17).

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            // Race vừa vào InProgress → đẩy snapshot cho spectator đang xem.
            // Đặt ở đây (thay vì trong StartRaceCommandHandler) để phủ CẢ endpoint thủ công
            // POST /start LẪN worker auto-start (RaceAutoStartBackgroundService) — cả hai đều
            // đi qua đúng hàm này.
            _liveTracker.MarkChanged(race.RaceId);

            return new RaceStartLifecycleResult(
                race.RaceId,
                RaceStartOutcome.Started,
                race.Status,
                race.NumberOfLegs,
                null,
                registrationWasClosed);
        }
        catch (DbUpdateException) when (!throwOnFailure)
        {
            // Hai worker race: PK Legs → coi như đã start.
            try { await tx.RollbackAsync(cancellationToken); } catch { /* ignore */ }
            return new RaceStartLifecycleResult(
                raceId,
                RaceStartOutcome.AlreadyStarted,
                RaceExecutionConstants.RaceInProgress,
                null,
                "Concurrent start detected.",
                RegistrationWasClosed: false);
        }
        catch
        {
            try { await tx.RollbackAsync(cancellationToken); } catch { /* ignore */ }
            throw;
        }
    }

    /// <summary>
    /// Auto-reject Pending, khóa Odds + GateNumber, set RegistrationCloseAt/OddsComputedAt.
    /// Caller phải đảm bảo race Scheduled, OddsComputedAt null, và ≥2 Approved.
    /// </summary>
    private async Task<(int approved, int rejected)> ApplyCloseRegistrationAsync(
        Race race,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await RaceLegProvisioner.EnsureLegsExistAsync(_context, race, cancellationToken);

        var legs = await _context.Legs.Where(l => l.RaceId == race.RaceId).ToListAsync(cancellationToken);
        foreach (var leg in legs)
        {
            leg.ExecutionStatus = LegExecutionStatuses.PredictionOpen;
            leg.PredictionOpenedAt = now;
        }
        
        var entries = race.Entries.Count > 0
            ? race.Entries.ToList()
            : await _context.Entries
                .Where(e => e.RaceId == race.RaceId)
                .ToListAsync(cancellationToken);

        var pending = entries.Where(e => e.Status == "Pending").ToList();
        foreach (var e in pending)
        {
            e.Status = "Rejected";
            e.RejectionReason = "Automatically rejected when registration closed (not approved).";
            e.UpdatedAt = now;
        }

        var approved = entries
            .Where(e => e.Status == RaceExecutionConstants.EntryApproved)
            .OrderBy(e => e.SubmittedAt)
            .ThenBy(e => e.EntryId)
            .ToList();

        if (approved.Count < 2)
            throw new InvalidOperationException(
                "At least 2 approved entries are required to close registration.");

        var horseIds = approved.Select(e => e.HorseId).Distinct().ToList();
        var history = await _context.RaceResults
            .Where(r => horseIds.Contains(r.Entry.HorseId) && r.FinalPosition != null)
            .Select(r => new { r.Entry.HorseId, r.FinalPosition })
            .ToListAsync(cancellationToken);

        var gate = 1;
        foreach (var e in approved)
        {
            var rows = history.Where(h => h.HorseId == e.HorseId).ToList();
            var firsts = rows.Count(h => h.FinalPosition == 1);
            e.Odds = CloseRegistrationCommandHandler.OddsFor(firsts, rows.Count, approved.Count);
            e.GateNumber = gate++;
            e.UpdatedAt = now;
        }

        race.RegistrationCloseAt = now;
        race.OddsComputedAt = now;
        race.UpdatedAt = now;

        return (approved.Count, pending.Count);
    }

    private async Task LockRaceRowAsync(int raceId, CancellationToken cancellationToken)
    {
        // Pessimistic lock — chỉ trên relational (PostgreSQL). InMemory/tests bỏ qua.
        if (!_context.Database.IsRelational())
            return;

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $@"SELECT ""RaceId"" FROM ""Races"" WHERE ""RaceId"" = {raceId} FOR UPDATE",
            cancellationToken);
    }

    private static async Task<RaceStartLifecycleResult> FailOrSkipAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx,
        int raceId,
        bool throwOnFailure,
        string reason,
        CancellationToken cancellationToken)
    {
        await tx.RollbackAsync(cancellationToken);

        if (throwOnFailure)
            throw new InvalidOperationException(reason);

        return new RaceStartLifecycleResult(
            raceId,
            RaceStartOutcome.Skipped,
            RaceExecutionConstants.RaceScheduled,
            null,
            reason,
            RegistrationWasClosed: false);
    }
}
