using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/legs/{legIndex}/override
// Admin resolve conflict: chốt kết quả chính thức (AdminOverride) + lý do bắt buộc, resume đua.
public sealed record OverrideLegResultCommand(
    int RaceId,
    int LegIndex,
    int AdminUserId,
    string? OverrideReason,
    IReadOnlyList<OverrideDecisionItem> Decisions) : ICommand<OverrideLegResultResponse>;

public sealed record OverrideDecisionItem(int EntryId, int OfficialPosition);

public sealed record OverrideLegResultResponse(
    int LegIndex,
    int LegNumber,
    string LegStatus,
    string RaceStatus,
    bool IsRaceComplete);

public sealed class OverrideLegResultCommandHandler
    : IRequestHandler<OverrideLegResultCommand, OverrideLegResultResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IRaceLiveChangeTracker _liveTracker;

    public OverrideLegResultCommandHandler(
        IApplicationDbContext context,
        IRaceLiveChangeTracker liveTracker)
    {
        _context = context;
        _liveTracker = liveTracker;
    }

    private async Task SettleLegPredictionsAsync(int raceId, int legNumber, DateTime now, CancellationToken ct)
    {
        var winnerResult = await _context.LegOfficialResults
            .FirstOrDefaultAsync(r => r.RaceId == raceId && r.LegNumber == legNumber && r.FinishPosition == 1, ct);

        if (winnerResult == null) return;

        var run = new SettlementRun
        {
            RaceId = raceId,
            Type = $"Leg{legNumber}Publish",
            Status = "Completed",
            CreatedAt = now
        };
        _context.SettlementRuns.Add(run);
        await _context.SaveChangesAsync(ct);

        var predictions = await _context.Predictions
            .Where(p => p.RaceId == raceId && p.LegNumber == legNumber && p.Status == PredictionStatus.Locked)
            .ToListAsync(ct);

        var spectatorIds = predictions.Select(p => p.SpectatorId).Distinct().ToList();
        var wallets = await _context.PointWallets.Where(w => spectatorIds.Contains(w.SpectatorId)).ToListAsync(ct);

        decimal totalBet = 0m;
        decimal totalPayout = 0m;

        foreach (var prediction in predictions)
        {
            var won = prediction.FirstEntryId == winnerResult.EntryId;
            var payout = won
                ? Math.Round(prediction.BetAmount * prediction.OddsLocked1, 2, MidpointRounding.AwayFromZero)
                : 0m;

            totalBet += prediction.BetAmount;
            totalPayout += payout;
            int? payoutTxId = null;

            if (won && payout > 0)
            {
                var wallet = wallets.FirstOrDefault(w => w.SpectatorId == prediction.SpectatorId);
                if (wallet is not null)
                {
                    wallet.Balance += payout;
                    wallet.UpdatedAt = now;

                    var payoutTx = new WalletTransaction
                    {
                        WalletId = wallet.WalletId,
                        SpectatorId = prediction.SpectatorId,
                        PredictionId = prediction.PredictionId,
                        SettlementRunId = run.SettlementRunId,
                        Type = "Payout",
                        Amount = payout,
                        BalanceAfter = wallet.Balance,
                        Reason = $"Payout Leg #{legNumber} Race #{raceId}",
                        CreatedAt = now
                    };
                    _context.WalletTransactions.Add(payoutTx);
                    await _context.SaveChangesAsync(ct);
                    payoutTxId = payoutTx.WalletTransactionId;
                }
            }

            _context.PredictionSettlements.Add(new PredictionSettlement
            {
                SettlementRunId = run.SettlementRunId,
                PredictionId = prediction.PredictionId,
                RaceId = raceId,
                SpectatorId = prediction.SpectatorId,
                MatchedCount = won ? 1 : 0,
                Outcome = won ? "Won" : "Lost",
                BetAmount = prediction.BetAmount,
                OddsAverage = prediction.OddsLocked1,
                PayoutAmount = payout,
                NetAmount = payout - prediction.BetAmount,
                PayoutTransactionId = payoutTxId,
                SettledAt = now
            });

            prediction.Status = won ? PredictionStatus.Won : PredictionStatus.Lost;
        }

        run.TotalPredictions = predictions.Count;
        run.TotalBetAmount = totalBet;
        run.TotalPayoutAmount = totalPayout;
    }

    public async Task<OverrideLegResultResponse> Handle(
        OverrideLegResultCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason))
            throw new InvalidOperationException("An override reason is required.");

        var legNumber = request.LegIndex + 1;

        var race = await _context.Races
                       .Include(r => r.Legs)
                       .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
                   ?? throw new KeyNotFoundException("Race not found.");

        var leg = race.Legs.FirstOrDefault(l => l.LegNumber == legNumber)
                  ?? throw new KeyNotFoundException("Leg not found.");

        if (leg.Status != RaceExecutionConstants.LegConflicted)
            throw new InvalidOperationException(
                $"Only a Conflicted leg can be resolved (current: {leg.Status}).");

        var approvedEntryIds = await _context.Entries
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .Select(e => e.EntryId)
            .ToListAsync(cancellationToken);

        var decisions = request.Decisions ?? new List<OverrideDecisionItem>();
        var decisionIds = decisions.Select(d => d.EntryId).ToHashSet();
        if (!decisionIds.SetEquals(approvedEntryIds))
            throw new InvalidOperationException("The decision does not match the approved entries.");

        var positiveRanks = decisions.Where(d => d.OfficialPosition > 0)
            .Select(d => d.OfficialPosition).ToList();
        if (positiveRanks.Count != positiveRanks.Distinct().Count())
            throw new InvalidOperationException("Duplicate ranking.");

        var now = DateTime.UtcNow;
        var reason = request.OverrideReason!.Trim();

        // Xóa official result cũ của leg (nếu có) rồi ghi lại theo quyết định Admin.
        var existing = await _context.LegOfficialResults
            .Where(o => o.RaceId == request.RaceId && o.LegNumber == legNumber)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
            _context.LegOfficialResults.RemoveRange(existing);

        foreach (var d in decisions)
        {
            var (finishPosition, resultStatus) =
                RaceExecutionConstants.DecodePosition(d.OfficialPosition);

            _context.LegOfficialResults.Add(new LegOfficialResult
            {
                RaceId = request.RaceId,
                LegNumber = legNumber,
                EntryId = d.EntryId,
                FinishPosition = finishPosition,
                ResultStatus = resultStatus,
                LegPoints = RaceExecutionConstants.LegPointsFor(finishPosition, resultStatus),
                ConfirmationType = RaceExecutionConstants.AdminOverride,
                ConfirmedAt = now,
                ConfirmedByAdminId = request.AdminUserId,
                OverrideReason = reason
            });
        }

        leg.Status = RaceExecutionConstants.LegResolved;
        leg.ConfirmationType = RaceExecutionConstants.AdminOverride;
        leg.AdminOverrideReason = reason;
        leg.ConfirmedAt = now;
        leg.FinishedAt = now;

        // Resume: nếu còn leg mở → InProgress; hết → PendingResult.
        var openLeg = race.Legs
            .Where(l => l.LegNumber != legNumber)
            .Any(l => l.Status != RaceExecutionConstants.LegConfirmed &&
                      l.Status != RaceExecutionConstants.LegResolved);

        race.Status = openLeg
            ? RaceExecutionConstants.RaceInProgress
            : RaceExecutionConstants.RacePendingResult;
        race.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        // Admin đã resolve → leg có vị trí chính thức (AdminOverride) + race hết Paused.
        _liveTracker.MarkChanged(request.RaceId);

        return new OverrideLegResultResponse(
            request.LegIndex,
            legNumber,
            leg.Status,
            race.Status,
            IsRaceComplete: !openLeg);
    }
}