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
    private readonly IReviewHistoryRepository _reviewHistoryRepository;
    private readonly IRaceLiveChangeTracker _liveTracker;

    public PublishRaceResultCommandHandler(
        IApplicationDbContext context,
        IReviewHistoryRepository reviewHistoryRepository,
        IRaceLiveChangeTracker liveTracker)
    {
        _context = context;
        _reviewHistoryRepository = reviewHistoryRepository;
        _liveTracker = liveTracker;
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
                    $"Only a PendingResult race can be published (current: {race.Status}).");

            var allLegsDone = race.Legs.Count > 0 && race.Legs.All(l =>
                l.Status is RaceExecutionConstants.LegConfirmed
                    or RaceExecutionConstants.LegResolved);
            if (!allLegsDone)
                throw new InvalidOperationException("There are still unconfirmed legs.");

            // Không cho publish khi còn vi phạm CHƯA duyệt: một Violation Pending
            // (DQ/Demote) nếu được duyệt sau đó sẽ thay đổi standings đã chốt (Flow 6 → 8).
            var pendingViolations = await _context.Violations
                .CountAsync(v => v.RaceId == race.RaceId && v.Status == "Pending", cancellationToken);
            if (pendingViolations > 0)
                throw new InvalidOperationException(
                    $"There are still {pendingViolations} unresolved violation(s). Please review them before publishing.");

            var beforeSnapshot = ReviewHistoryJson.Serialize(new
            {
                raceId = race.RaceId,
                name = race.Name,
                status = race.Status,
                publishedAt = race.PublishedAt
            });

            var entries = await _context.Entries
                .Where(e => e.RaceId == race.RaceId &&
                            e.Status == RaceExecutionConstants.EntryApproved)
                .ToListAsync(cancellationToken);

            var officials = await _context.LegOfficialResults
                .Where(o => o.RaceId == race.RaceId)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;

            // Entry bị Race DQ (vi phạm đã duyệt) — xếp cuối, 0 điểm, 0 Prize (Flow 6).
            var dqSet = (await _context.Violations
                .Where(v => v.RaceId == race.RaceId && v.Status == "Approved" && v.Penalty == "DQ")
                .Select(v => v.EntryId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();

            // ── 1. Tính tổng điểm & xếp hạng (DQ luôn xuống đáy) ──
            // Công thức chung với màn "xem trước" (ReviewRacePublicationQueryHandler)
            // để bảng điểm trước publish khớp tuyệt đối với kết quả publish thật.
            var ranked = RaceRankingCalculator.Rank(entries, officials, dqSet);

            // ── 2. Ghi RaceResult ──
            int? winnerEntryId = null;
            foreach (var r in ranked)
            {
                if (winnerEntryId is null && !r.IsDq) winnerEntryId = r.Entry.EntryId;

                _context.RaceResults.Add(new RaceResult
                {
                    RaceId = race.RaceId,
                    EntryId = r.Entry.EntryId,
                    TotalPoints = r.TotalPoints,
                    FinalPosition = r.FinalPosition,
                    IsRaceDQ = r.IsDq,
                    LegWinCount = r.LegWins,
                    LegTop3Count = r.LegTop3,
                    PublishedAt = now,
                    CreatedAt = now
                });
            }

            await _context.SaveChangesAsync(
                cancellationToken); // RaceResult tồn tại trước khi PrizePointTransaction tham chiếu FK

            // ── 3. Cộng Prize Points cho Owner & Jockey (bỏ qua entry DQ) ──
            foreach (var r in ranked)
            {
                if (r.IsDq)
                {
                    continue;
                }

                var finalPosition = r.FinalPosition;
                var prize = RaceExecutionConstants.PrizePointsFor(finalPosition);

                if (prize <= 0)
                {
                    continue;
                }

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
                        FinalPosition = finalPosition,
                        Points = prize,
                        TransactionType = PrizePointTransactionType.Awarded,
                        CreatedAt = now
                    });
                }
            }

            // ── 3b. Cập nhật thống kê Career của Jockey (nếu có hồ sơ) ──
            var jockeyIds = ranked.Select(r => r.Entry.JockeyId).Distinct().ToList();
            var profiles = await _context.JockeyProfiles
                .Where(p => jockeyIds.Contains(p.UserId))
                .ToListAsync(cancellationToken);
            foreach (var r in ranked)
            {
                var profile = profiles.FirstOrDefault(p => p.UserId == r.Entry.JockeyId);
                if (profile is not null)
                {
                    profile.TotalRaces += 1;
                    if (!r.IsDq && r.FinalPosition == 1) profile.TotalWins += 1;
                    if (!r.IsDq && r.FinalPosition <= 3) profile.TotalTop3 += 1;
                    profile.CareerPrizePoints += r.IsDq ? 0 : RaceExecutionConstants.PrizePointsFor(r.FinalPosition);
                    profile.UpdatedAt = now;
                }
            }

            // ── 4. Quyết toán dự đoán ──
            var run = new SettlementRun
            {
                RaceId = race.RaceId,
                Type = "Publish",
                Status = "Completed",
                TriggeredByAdminId = request.AdminUserId,
                CreatedAt = now,
                TotalPredictions = 0,
                TotalBetAmount = 0m,
                TotalPayoutAmount = 0m
            };
            _context.SettlementRuns.Add(run);
            await _context.SaveChangesAsync(cancellationToken);

            var settledPredictionsCount = 0;
            var totalPayout = 0m;

            // var run = new SettlementRun
            // {
            //     RaceId = race.RaceId,
            //     Type = "Publish",
            //     Status = "Completed",
            //     TriggeredByAdminId = request.AdminUserId,
            //     CreatedAt = now
            // };
            //
            // _context.SettlementRuns.Add(run);
            // await _context.SaveChangesAsync(cancellationToken); // lấy SettlementRunId
            //
            // var predictions = await _context.Predictions
            //     .Where(p =>
            //         p.RaceId == race.RaceId &&
            //         (
            //             p.Status == PredictionStatus.Pending ||
            //             p.Status == PredictionStatus.Locked
            //         ))
            //     .ToListAsync(cancellationToken);
            //
            // var spectatorIds = predictions
            //     .Select(p => p.SpectatorId)
            //     .Distinct()
            //     .ToList();
            //
            // var wallets = await _context.PointWallets
            //     .Where(w => spectatorIds.Contains(w.SpectatorId))
            //     .ToListAsync(cancellationToken);
            //
            // decimal totalBet = 0m;
            // decimal totalPayout = 0m;
            //
            // foreach (var prediction in predictions)
            // {
            //     var won = winnerEntryId is not null &&
            //               prediction.FirstEntryId == winnerEntryId.Value;
            //
            //     var payout = won
            //         ? Math.Round(
            //             prediction.BetAmount * prediction.OddsLocked1,
            //             2,
            //             MidpointRounding.AwayFromZero)
            //         : 0m;
            //
            //     totalBet += prediction.BetAmount;
            //     totalPayout += payout;
            //
            //     int? payoutTxId = null;
            //
            //     if (won && payout > 0)
            //     {
            //         var wallet = wallets.FirstOrDefault(w =>
            //             w.SpectatorId == prediction.SpectatorId);
            //
            //         if (wallet is null)
            //         {
            //             throw new InvalidOperationException(
            //                 $"Wallet not found for spectator #{prediction.SpectatorId}.");
            //         }
            //
            //         // Settlement là giao dịch hệ thống.
            //         // Vẫn credit payout để kết quả tài chính đúng.
            //         wallet.Balance += payout;
            //         wallet.UpdatedAt = now;
            //
            //         var payoutTx = new WalletTransaction
            //         {
            //             WalletId = wallet.WalletId,
            //             SpectatorId = prediction.SpectatorId,
            //             PredictionId = prediction.PredictionId,
            //             SettlementRunId = run.SettlementRunId,
            //             Type = "Payout",
            //             Amount = payout,
            //             BalanceAfter = wallet.Balance,
            //             Reason =
            //                 $"Won bet on race #{race.RaceId}, entry #{prediction.FirstEntryId}, odds {prediction.OddsLocked1}",
            //             CreatedAt = now
            //         };
            //
            //         _context.WalletTransactions.Add(payoutTx);
            //
            //         await _context.SaveChangesAsync(cancellationToken);
            //
            //         payoutTxId = payoutTx.WalletTransactionId;
            //     }
            //
            //     _context.PredictionSettlements.Add(new PredictionSettlement
            //     {
            //         SettlementRunId = run.SettlementRunId,
            //         PredictionId = prediction.PredictionId,
            //         RaceId = race.RaceId,
            //         SpectatorId = prediction.SpectatorId,
            //         MatchedCount = won ? 1 : 0,
            //         Outcome = won ? "Won" : "Lost",
            //         BetAmount = prediction.BetAmount,
            //         OddsAverage = prediction.OddsLocked1,
            //         PayoutAmount = payout,
            //         NetAmount = payout - prediction.BetAmount,
            //         PayoutTransactionId = payoutTxId,
            //         SettledAt = now,
            //         IsRollbacked = false
            //     });
            //
            //     prediction.Status = won
            //         ? PredictionStatus.Won
            //         : PredictionStatus.Lost;
            // }
            //
            // run.TotalPredictions = predictions.Count;
            // run.TotalBetAmount = totalBet;
            // run.TotalPayoutAmount = totalPayout;
            
// 1. Lấy danh sách ID các ngựa đã thực tế tham gia cuộc đua
            var participantHorseIds = entries.Select(e => e.HorseId).Distinct().ToList();
// 2. Trừ 1 điểm thể lực của tất cả ngựa tham gia đua (Tối thiểu về 0)
            var participantHorses = await _context.Horses
                .Where(h => participantHorseIds.Contains(h.HorseId))
                .ToListAsync(cancellationToken);
            foreach (var h in participantHorses)
            {
                h.Stamina = Math.Max(0, h.Stamina - 1);
                h.UpdatedAt = now;
            }

// 3. Hồi phục full 3 điểm thể lực cho các ngựa hoạt động khác (Nghỉ ngơi không đua trận này)
            var restingHorses = await _context.Horses
                .Where(h => h.Status == "Approved" && !participantHorseIds.Contains(h.HorseId))
                .ToListAsync(cancellationToken);
            foreach (var h in restingHorses)
            {
                h.Stamina = 3;
                h.UpdatedAt = now;
            }

// Lưu thay đổi thể lực xuống database
            await _context.SaveChangesAsync(cancellationToken);
            // ── 5. Chốt race ──
            race.Status = RaceExecutionConstants.RaceFinished;
            race.PublishedAt = now;
            race.UpdatedAt = now;

            await _reviewHistoryRepository.AddAsync(
                new ReviewHistory
                {
                    EntityType = ReviewEntity.Race,
                    EntityId = race.RaceId,
                    Action = ReviewAction.Published,
                    Reason = null,
                    BeforeData = beforeSnapshot,
                    AfterData = ReviewHistoryJson.Serialize(new
                    {
                        raceId = race.RaceId,
                        name = race.Name,
                        status = race.Status,
                        publishedAt = race.PublishedAt,
                        settlementRunId = run.SettlementRunId,
                        resultsCount = ranked.Count,
                        settledPredictions = settledPredictionsCount,
                        totalPayout = totalPayout
                    }),
                    AdminId = request.AdminUserId
                },
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            // Race → Finished. Đánh dấu ở handler (không phải controller) nên phủ cả
            // POST /api/races/{id}/publish lẫn POST /api/admin/races/{id}/publish (2 route trùng nhau).
            _liveTracker.MarkChanged(race.RaceId);

            return new PublishRaceResultResponse(
                race.RaceId,
                race.Status,
                ranked.Count,
                settledPredictionsCount,
                totalPayout);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}