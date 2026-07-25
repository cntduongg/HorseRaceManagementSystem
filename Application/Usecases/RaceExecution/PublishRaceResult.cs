using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

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

            var dqSet = (await _context.Violations
                .Where(v => v.RaceId == race.RaceId && v.Status == "Approved" && v.Penalty == "DQ")
                .Select(v => v.EntryId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();

            var ranked = RaceRankingCalculator.Rank(entries, officials, dqSet);

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

            await _context.SaveChangesAsync(cancellationToken);

            // Trao giải Prize Points cho Owner/Jockey
            foreach (var r in ranked)
            {
                if (r.IsDq) continue;

                var finalPosition = r.FinalPosition;
                var prize = RaceExecutionConstants.PrizePointsFor(finalPosition, entries.Count);

                if (prize <= 0) continue;

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

            // Cập nhật thống kê Jockey Career
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
                    profile.CareerPrizePoints += r.IsDq ? 0 : RaceExecutionConstants.PrizePointsFor(r.FinalPosition, entries.Count);
                    profile.UpdatedAt = now;
                }
            }

            // ── Quyết toán cược (Race-scoped) ──
            var run = new SettlementRun
            {
                RaceId = race.RaceId,
                Type = "Publish",
                Status = "Completed",
                TriggeredByAdminId = request.AdminUserId,
                CreatedAt = now
            };

            _context.SettlementRuns.Add(run);
            await _context.SaveChangesAsync(cancellationToken);

            var predictions = await _context.Predictions
                .Where(p =>
                    p.RaceId == race.RaceId &&
                    (
                        p.Status == PredictionStatus.Pending ||
                        p.Status == PredictionStatus.Locked
                    ))
                .ToListAsync(cancellationToken);

            var spectatorIds = predictions
                .Select(p => p.SpectatorId)
                .Distinct()
                .ToList();

            var wallets = await _context.PointWallets
                .Where(w => spectatorIds.Contains(w.SpectatorId))
                .ToListAsync(cancellationToken);

            decimal totalBet = 0m;
            decimal totalPayout = 0m;

            foreach (var prediction in predictions)
            {
                var won = winnerEntryId is not null &&
                          prediction.FirstEntryId == winnerEntryId.Value;

                var payout = won
                    ? Math.Round(
                        prediction.BetAmount * prediction.OddsLocked1,
                        2,
                        MidpointRounding.AwayFromZero)
                    : 0m;

                totalBet += prediction.BetAmount;
                totalPayout += payout;

                int? payoutTxId = null;

                if (won && payout > 0)
                {
                    var wallet = wallets.FirstOrDefault(w =>
                        w.SpectatorId == prediction.SpectatorId);

                    if (wallet is null)
                    {
                        throw new InvalidOperationException(
                            $"Wallet not found for spectator #{prediction.SpectatorId}.");
                    }

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
                        Reason =
                            $"Won bet on race #{race.RaceId}, entry #{prediction.FirstEntryId}, odds {prediction.OddsLocked1}",
                        CreatedAt = now
                    };

                    _context.WalletTransactions.Add(payoutTx);

                    await _context.SaveChangesAsync(cancellationToken);

                    payoutTxId = payoutTx.WalletTransactionId;
                }

                _context.PredictionSettlements.Add(new PredictionSettlement
                {
                    SettlementRunId = run.SettlementRunId,
                    PredictionId = prediction.PredictionId,
                    RaceId = race.RaceId,
                    SpectatorId = prediction.SpectatorId,
                    MatchedCount = won ? 1 : 0,
                    Outcome = won ? "Won" : "Lost",
                    BetAmount = prediction.BetAmount,
                    OddsAverage = prediction.OddsLocked1,
                    PayoutAmount = payout,
                    NetAmount = payout - prediction.BetAmount,
                    PayoutTransactionId = payoutTxId,
                    SettledAt = now,
                    IsRollbacked = false
                });

                prediction.Status = won
                    ? PredictionStatus.Won
                    : PredictionStatus.Lost;
            }

            run.TotalPredictions = predictions.Count;
            run.TotalBetAmount = totalBet;
            run.TotalPayoutAmount = totalPayout;

            // --- KHÔNG CÓ PHẦN KHẤU TRỪ THỂ LỰC NGỰA (ĐÃ XÓA) ---

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
                        settledPredictions = predictions.Count,
                        totalPayout
                    }),
                    AdminId = request.AdminUserId
                },
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            _liveTracker.MarkChanged(race.RaceId);

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