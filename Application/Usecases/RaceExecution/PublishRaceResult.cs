using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/publish — Admin công bố kết quả + quyết toán cược (ATOMIC).
public sealed record PublishRaceResultCommand(int RaceId, int AdminUserId)
    : ICommand<PublishRaceResultResponse>;

public sealed record PublishRaceResultResponse(
    int RaceId,
    string Status,
    int ResultsCount,
    int SettledPredictions,
    decimal TotalPayout);

public sealed class PublishRaceResultCommandHandler
    : IRequestHandler<PublishRaceResultCommand, PublishRaceResultResponse>
{
    private readonly IApplicationDbContext _context;

    public PublishRaceResultCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublishRaceResultResponse> Handle(
        PublishRaceResultCommand request,
        CancellationToken cancellationToken)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var race = await _context.Races
                .Include(r => r.Legs)
                .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
                ?? throw new KeyNotFoundException("Race not found.");

            if (race.Status != RaceExecutionConstants.RacePendingResult)
                throw new InvalidOperationException(
                    $"Chỉ publish được race ở PendingResult (hiện tại: {race.Status}).");

            var allLegsDone = race.Legs.Count > 0 && race.Legs.All(l =>
                l.Status is RaceExecutionConstants.LegConfirmed
                          or RaceExecutionConstants.LegResolved);
            if (!allLegsDone)
                throw new InvalidOperationException("Vẫn còn leg chưa được xác nhận.");

            var entries = await _context.Entries
                .Where(e => e.RaceId == race.RaceId &&
                            e.Status == RaceExecutionConstants.EntryApproved)
                .ToListAsync(cancellationToken);

            var officials = await _context.LegOfficialResults
                .Where(o => o.RaceId == race.RaceId)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;

            // ── 1. Tính tổng điểm & xếp hạng ──
            var ranked = entries.Select(e =>
            {
                var rows = officials.Where(o => o.EntryId == e.EntryId).ToList();
                return new
                {
                    Entry = e,
                    TotalPoints = rows.Sum(r => r.LegPoints),
                    LegWins = rows.Count(r => r.ResultStatus == RaceExecutionConstants.ResultFinished && r.FinishPosition == 1),
                    LegTop3 = rows.Count(r => r.ResultStatus == RaceExecutionConstants.ResultFinished && r.FinishPosition is >= 1 and <= 3)
                };
            })
            .OrderByDescending(x => x.TotalPoints)
            .ThenByDescending(x => x.LegWins)
            .ThenByDescending(x => x.LegTop3)
            .ToList();

            // ── 2. Ghi RaceResult ──
            var position = 1;
            int? winnerEntryId = null;
            foreach (var r in ranked)
            {
                if (position == 1) winnerEntryId = r.Entry.EntryId;

                _context.RaceResults.Add(new RaceResult
                {
                    RaceId = race.RaceId,
                    EntryId = r.Entry.EntryId,
                    TotalPoints = r.TotalPoints,
                    FinalPosition = position,
                    LegWinCount = r.LegWins,
                    LegTop3Count = r.LegTop3,
                    PublishedAt = now,
                    CreatedAt = now
                });
                position++;
            }
            await _context.SaveChangesAsync(cancellationToken); // RaceResult tồn tại trước khi PrizePointTransaction tham chiếu FK

            // ── 3. Cộng Prize Points cho Owner & Jockey ──
            position = 1;
            foreach (var r in ranked)
            {
                var prize = RaceExecutionConstants.PrizePointsFor(position);
                if (prize > 0)
                {
                    foreach (var (userId, source) in new[]
                    {
                        (r.Entry.HorseOwnerId, "OwnerPrize"),
                        (r.Entry.JockeyId, "JockeyPrize")
                    })
                    {
                        _context.PrizePointTransactions.Add(new PrizePointTransaction
                        {
                            TournamentId = race.TournamentId,
                            RaceId = race.RaceId,
                            EntryId = r.Entry.EntryId,
                            UserId = userId,
                            SourceType = source,
                            FinalPosition = position,
                            Points = prize,
                            TransactionType = PrizePointTransactionType.Awarded,
                            CreatedAt = now
                        });
                    }
                }
                position++;
            }

            // ── 4. Quyết toán dự đoán ──
            var run = new SettlementRun
            {
                RaceId = race.RaceId,
                Type = "Publish",
                Status = "Completed",
                TriggeredByAdminId = request.AdminUserId,
                CreatedAt = now
            };
            _context.SettlementRuns.Add(run);
            await _context.SaveChangesAsync(cancellationToken); // lấy SettlementRunId

            var predictions = await _context.Predictions
                .Where(p => p.RaceId == race.RaceId && p.Status == "Pending")
                .ToListAsync(cancellationToken);

            var spectatorIds = predictions.Select(p => p.SpectatorId).Distinct().ToList();
            var wallets = await _context.PointWallets
                .Where(w => spectatorIds.Contains(w.SpectatorId))
                .ToListAsync(cancellationToken);

            decimal totalBet = 0, totalPayout = 0;
            foreach (var p in predictions)
            {
                var won = winnerEntryId != null && p.FirstEntryId == winnerEntryId;
                var payout = won ? Math.Round(p.BetAmount * p.OddsLocked1, 2) : 0m;

                totalBet += p.BetAmount;
                totalPayout += payout;

                int? payoutTxId = null;
                if (won && payout > 0)
                {
                    var wallet = wallets.FirstOrDefault(w => w.SpectatorId == p.SpectatorId);
                    if (wallet is { IsFrozen: false })
                    {
                        wallet.Balance += payout;
                        wallet.UpdatedAt = now;

                        var payoutTx = new WalletTransaction
                        {
                            WalletId = wallet.WalletId,
                            SpectatorId = p.SpectatorId,
                            PredictionId = p.PredictionId,
                            SettlementRunId = run.SettlementRunId,
                            Type = "Payout",
                            Amount = payout,
                            BalanceAfter = wallet.Balance,
                            Reason = $"Thắng cược race #{race.RaceId}",
                            CreatedAt = now
                        };
                        _context.WalletTransactions.Add(payoutTx);
                        await _context.SaveChangesAsync(cancellationToken);
                        payoutTxId = payoutTx.WalletTransactionId;
                    }
                }

                _context.PredictionSettlements.Add(new PredictionSettlement
                {
                    SettlementRunId = run.SettlementRunId,
                    PredictionId = p.PredictionId,
                    RaceId = race.RaceId,
                    SpectatorId = p.SpectatorId,
                    MatchedCount = won ? 1 : 0,
                    Outcome = won ? "Won" : "Lost",
                    BetAmount = p.BetAmount,
                    OddsAverage = p.OddsLocked1,
                    PayoutAmount = payout,
                    NetAmount = payout - p.BetAmount,
                    PayoutTransactionId = payoutTxId,
                    SettledAt = now
                });

                p.Status = won ? "Won" : "Lost";
            }

            run.TotalPredictions = predictions.Count;
            run.TotalBetAmount = totalBet;
            run.TotalPayoutAmount = totalPayout;

            // ── 5. Chốt race ──
            race.Status = RaceExecutionConstants.RaceFinished;
            race.PublishedAt = now;
            race.UpdatedAt = now;

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new PublishRaceResultResponse(
                race.RaceId,
                race.Status,
                ranked.Count,
                predictions.Count,
                totalPayout);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
