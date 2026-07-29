using Application.Common.Interfaces;
using Application.Common.Wallet;
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

public enum RaceCancelOutcome
{
    Cancelled,
    Skipped
}
public sealed record RaceCancelLifecycleResult(
    int RaceId,
    RaceCancelOutcome Outcome,
    int WithdrawnEntries,
    int CancelledInvitations,
    int RefundedPredictions,
    string? SkipReason);

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
    /// <remarks>
    /// Cược tự khóa (<c>Pending → Locked</c>) ngay trong hàm này — không có bước "Lock Betting"
    /// riêng để Admin/Referee phải bấm trước (Flow 7).
    /// </remarks>
    Task<RaceStartLifecycleResult> StartRaceAsync(
        int raceId,
        bool enforceSchedule,
        bool allowAutoClose,
        bool throwOnFailure,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Cancel Race (chỉ khi đang Scheduled) + cascade: Entry Pending/Approved → Withdrawn,
    /// JockeyInvitation Pending/Accepted/Confirmed → Cancelled. Dùng chung bởi endpoint
    /// Delete thủ công (throwOnFailure=true) và worker auto-cancel (throwOnFailure=false).
    /// </summary>
    Task<RaceCancelLifecycleResult> CancelRaceAsync(
        int raceId,
        string reason,
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
    public async Task<RaceCancelLifecycleResult> CancelRaceAsync(
        int raceId,
        string reason,
        bool throwOnFailure,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await LockRaceRowAsync(raceId, cancellationToken);

            var race = await _context.Races
                .FirstOrDefaultAsync(r => r.RaceId == raceId, cancellationToken)
                ?? throw new KeyNotFoundException("Race not found.");

            if (race.Status != RaceExecutionConstants.RaceScheduled)
            {
                await tx.RollbackAsync(cancellationToken);

                var skipReason =
                    $"Only scheduled races can be cancelled (current: {race.Status}).";

                if (throwOnFailure)
                    throw new InvalidOperationException(skipReason);

                return new RaceCancelLifecycleResult(
                    raceId, RaceCancelOutcome.Skipped, 0, 0, 0, skipReason);
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            race.Status = RaceExecutionConstants.RaceCancelled;
            race.UpdatedAt = now;

            var entries = await _context.Entries
                .Where(e => e.RaceId == raceId &&
                    (e.Status == EntryStatus.Pending || e.Status == EntryStatus.Approved))
                .ToListAsync(cancellationToken);

            foreach (var entry in entries)
            {
                entry.Status = "Withdrawn";
                entry.RejectionReason = reason;
                entry.UpdatedAt = now;
            }

            var invitations = await _context.JockeyInvitations
                .Where(i => i.RaceId == raceId &&
                    (i.Status == "Pending" || i.Status == "Accepted" || i.Status == "Confirmed"))
                .ToListAsync(cancellationToken);

            foreach (var invitation in invitations)
            {
                invitation.Status = "Cancelled";
                invitation.CancelledAt = now;
            }

            // Cuộc đua không diễn ra thì mọi lệnh cược phải trả lại tiền. Nếu không, prediction
            // đứng mãi ở Pending/Locked: settlement chỉ chạy trong PublishRaceResult, mà race
            // đã Cancelled thì không bao giờ publish ⇒ tiền của spectator kẹt vĩnh viễn.
            var refunded = await RefundPredictionsForCancelledRaceAsync(race, now, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new RaceCancelLifecycleResult(
                race.RaceId,
                RaceCancelOutcome.Cancelled,
                entries.Count,
                invitations.Count,
                refunded,
                null);
        }
        catch (KeyNotFoundException)
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
        catch
        {
            try { await tx.RollbackAsync(cancellationToken); } catch { /* ignore */ }
            throw;
        }
    }
    /// <summary>
    /// Hoàn 100% điểm cược cho mọi prediction còn sống của một race vừa bị hủy.
    /// </summary>
    /// <remarks>
    /// Cùng cách xử lý với <c>RevokeHorseCommandHandler.RefundPredictionsForEntryAsync</c>, chỉ
    /// khác bộ lọc: ở đây là cả race chứ không riêng một Entry.
    ///
    /// Ví đang <c>IsFrozen</c> (chủ tài khoản bị Admin khóa) thì prediction vẫn chuyển
    /// <c>Cancelled</c> nhưng KHÔNG cộng tiền — đóng băng ví là cố ý chặn mọi biến động số dư,
    /// cộng vào đây sẽ phá đúng cái mà LockUser vừa làm. Tiền được trả lại khi Admin unlock.
    /// </remarks>
    /// <returns>Số prediction đã hủy (kể cả trường hợp ví đóng băng không cộng được tiền).</returns>
    private async Task<int> RefundPredictionsForCancelledRaceAsync(
        Race race,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // `!` trên FirstEntry: navigation khai báo nullable nhưng FK là bắt buộc, và ThenInclude
        // cần kiểu non-null để không sinh CS8602.
        var predictions = await _context.Predictions
            .Include(p => p.FirstEntry!)
                .ThenInclude(e => e.Horse)
            .Include(p => p.FirstEntry!)
                .ThenInclude(e => e.Jockey)
            .Where(p => p.RaceId == race.RaceId &&
                        (p.Status == PredictionStatus.Pending || p.Status == PredictionStatus.Locked))
            .ToListAsync(cancellationToken);

        if (predictions.Count == 0)
            return 0;

        var spectatorIds = predictions.Select(p => p.SpectatorId).Distinct().ToList();
        var wallets = await _context.PointWallets
            .Where(w => spectatorIds.Contains(w.SpectatorId))
            .ToListAsync(cancellationToken);

        foreach (var prediction in predictions)
        {
            prediction.Status = PredictionStatus.Cancelled;
            prediction.CancelledAt = now;

            var wallet = wallets.FirstOrDefault(w => w.SpectatorId == prediction.SpectatorId);
            if (wallet is null || wallet.IsFrozen)
                continue;

            wallet.Balance += prediction.BetAmount;
            wallet.UpdatedAt = now;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.WalletId,
                SpectatorId = prediction.SpectatorId,
                PredictionId = prediction.PredictionId,
                Type = "BetRefund",
                Amount = prediction.BetAmount,
                BalanceAfter = wallet.Balance,
                Reason = WalletTransactionReasonBuilder.BetRefundRaceCancelled(
                    race,
                    prediction.FirstEntry!.Horse,
                    prediction.FirstEntry.Jockey),
                CreatedAt = now
            });
        }

        return predictions.Count;
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
                        "Registration must be closed before starting the race.",
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

            // Race xuất phát = Leg 1 xuất phát. Trước đây StartedAt chỉ được ghi lúc referee
            // ĐẦU TIÊN nộp kết quả (SubmitLegResult), tức sau khi chặng đã chạy xong — nên
            // trang Live của spectator hiện "Not started" suốt cả chặng đang đua.
            // Đọc từ Local vì leg vừa tạo trong EnsureLegsExistAsync chưa SaveChanges (query
            // xuống DB sẽ không thấy), còn leg cũ đã nằm sẵn trong tracker qua Include(r => r.Legs).
            var firstLeg = _context.Legs.Local
                .Where(l => l.RaceId == race.RaceId)
                .OrderBy(l => l.LegNumber)
                .FirstOrDefault();
            if (firstLeg is not null && firstLeg.StartedAt is null)
                firstLeg.StartedAt = now;

            // Xuất phát là đóng sổ cược: mọi lệnh còn Pending chuyển sang Locked ngay tại đây.
            // Đây là cơ chế khóa DUY NHẤT — không có nút "Lock Betting" nào phải bấm trước.
            var pendingPredictions = await _context.Predictions
                .Where(p => p.RaceId == raceId && p.Status == PredictionStatus.Pending)
                .ToListAsync(cancellationToken);
            foreach (var prediction in pendingPredictions)
                prediction.Status = PredictionStatus.Locked;

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
    /// Auto-reject Pending, sinh Odds + GateNumber (một lần duy nhất, qua
    /// <see cref="RaceOddsAssigner"/>), set RegistrationCloseAt/OddsComputedAt.
    /// Caller phải đảm bảo race Scheduled, OddsComputedAt null, và ≥2 Approved.
    /// </summary>
    private async Task<(int approved, int rejected)> ApplyCloseRegistrationAsync(
        Race race,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await RaceLegProvisioner.EnsureLegsExistAsync(_context, race, cancellationToken);

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

        // Đây là thời điểm DUY NHẤT odds được sinh ra. Từ đây tới lúc race kết thúc, con số
        // này không đổi nữa — spectator cược đúng nó, Prediction khóa đúng nó (Flow 7).
        await RaceOddsAssigner.AssignAsync(_context, race.RaceId, approved, now, cancellationToken);

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
