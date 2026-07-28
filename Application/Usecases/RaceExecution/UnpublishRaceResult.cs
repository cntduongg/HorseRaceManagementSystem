using System.Text.Json;
using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Common.Wallet;
namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/unpublish — Admin rollback kết quả + payout (ATOMIC) → PendingResult.
public sealed record UnpublishRaceResultCommand(int RaceId, int AdminUserId, string Reason)
    : ICommand<UnpublishRaceResultResponse>;

public sealed record UnpublishRaceResultResponse(
    int RaceId,
    string Status,
    int ReversedPayouts);

public sealed class UnpublishRaceResultCommandHandler
    : IRequestHandler<UnpublishRaceResultCommand, UnpublishRaceResultResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;
    private readonly IRaceLiveChangeTracker _liveTracker;
    private readonly ILogger<UnpublishRaceResultCommandHandler> _logger;

    public UnpublishRaceResultCommandHandler(
        IApplicationDbContext context,
        IReviewHistoryRepository reviewHistoryRepository,
        IRaceLiveChangeTracker liveTracker,
        ILogger<UnpublishRaceResultCommandHandler> logger)
    {
        _context = context;
        _reviewHistoryRepository = reviewHistoryRepository;
        _liveTracker = liveTracker;
        _logger = logger;
    }

    public async Task<UnpublishRaceResultResponse> Handle(
        UnpublishRaceResultCommand request,
        CancellationToken cancellationToken)
    {
        var reason = ReviewHistoryReason.Normalize(
            request.Reason,
            required: true,
            fieldName: "Unpublish reason")!;

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var race = await _context.Races
                .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
                ?? throw new KeyNotFoundException("Race not found.");

            if (race.Status != RaceExecutionConstants.RaceFinished)
                throw new InvalidOperationException(
                    $"Only a Finished race can be unpublished (current: {race.Status}).");

            var resultsCount = await _context.RaceResults
                .CountAsync(r => r.RaceId == race.RaceId, cancellationToken);

            var beforeSnapshot = ReviewHistoryJson.Serialize(new
            {
                raceId = race.RaceId,
                name = race.Name,
                status = race.Status,
                publishedAt = race.PublishedAt,
                resultsCount
            });

            var now = DateTime.UtcNow;

            // ── 1. Hoàn lại payout đã chi ──
            var settlements = await _context.PredictionSettlements
            .Include(s => s.Prediction)
                .ThenInclude(p => p!.Race)
            .Include(s => s.Prediction)
                .ThenInclude(p => p!.FirstEntry)
                    .ThenInclude(e => e.Horse)
            .Include(s => s.Prediction)
                .ThenInclude(p => p!.FirstEntry)
                    .ThenInclude(e => e.Jockey)
            .Where(s => s.RaceId == race.RaceId && !s.IsRollbacked)
            .ToListAsync(cancellationToken);

            var spectatorIds = settlements.Select(s => s.SpectatorId).Distinct().ToList();
            var wallets = await _context.PointWallets
                .Where(w => spectatorIds.Contains(w.SpectatorId))
                .ToListAsync(cancellationToken);

            var reversed = 0;
            foreach (var s in settlements)
            {
                if (s.Outcome == "Won" && s.PayoutAmount > 0 && s.PayoutTransactionId != null)
                {
                    var wallet = wallets.FirstOrDefault(w => w.SpectatorId == s.SpectatorId);
                    if (wallet is null)
                    {
                        throw new InvalidOperationException(
                            $"Wallet not found for spectator #{s.SpectatorId}.");
                    }

                    wallet.Balance -= s.PayoutAmount;
                    wallet.UpdatedAt = now;

                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        WalletId = wallet.WalletId,
                        SpectatorId = s.SpectatorId,
                        PredictionId = s.PredictionId,
                        SettlementRunId = s.SettlementRunId,
                        Type = "PayoutRollback",
                        Amount = -s.PayoutAmount,
                        BalanceAfter = wallet.Balance,
                        Reason = WalletTransactionReasonBuilder.PayoutRollback(
                        s.Prediction!.Race!,
                        s.Prediction.FirstEntry!.Horse,
                        s.Prediction.FirstEntry.Jockey),
                        RollbackOfTransactionId = s.PayoutTransactionId,
                        CreatedAt = now
                    });
                    reversed++;
                }

                s.IsRollbacked = true;
                s.RollbackAt = now;
            }

            // ── 2. Đưa dự đoán về Pending ──
            var predictionIds = settlements.Select(s => s.PredictionId).ToList();
            var predictions = await _context.Predictions
                .Where(p => predictionIds.Contains(p.PredictionId))
                .ToListAsync(cancellationToken);
            foreach (var p in predictions)
            {
                if (p.Status == PredictionStatus.Won ||
                    p.Status == PredictionStatus.Lost)
                {
                    p.Status = PredictionStatus.Locked;
                }
            }

            // ── 3. Đánh dấu các SettlementRun đã rollback ──
            var runs = await _context.SettlementRuns
                .Where(r => r.RaceId == race.RaceId && r.Status == "Completed")
                .ToListAsync(cancellationToken);
            foreach (var run in runs)
                run.Status = "RolledBack";

            // ── 4. Gỡ Prize Points & RaceResult (Prize trước vì FK tới RaceResult) ──
            var prizes = await _context.PrizePointTransactions
                .Where(p => p.RaceId == race.RaceId)
                .ToListAsync(cancellationToken);
            _context.PrizePointTransactions.RemoveRange(prizes);

            var results = await _context.RaceResults
                .Where(r => r.RaceId == race.RaceId)
                .ToListAsync(cancellationToken);

            // ── 4b. Hoàn lại thống kê Career của Jockey (đối xứng với Publish) ──
            var entries = await _context.Entries
                .Where(e => e.RaceId == race.RaceId)
                .Select(e => new { e.EntryId, e.JockeyId, e.Status })
                .ToListAsync(cancellationToken);

            // Phải dùng ĐÚNG sĩ số mà Publish đã dùng (số Entry Approved), nếu không
            // rollback Prize Points sẽ lệch so với lúc cộng.
            var fieldSize = entries.Count(e => e.Status == RaceExecutionConstants.EntryApproved);
            var jockeyByEntry = entries.ToDictionary(e => e.EntryId, e => e.JockeyId);
            var jockeyIds = entries.Select(e => e.JockeyId).Distinct().ToList();
            var profiles = await _context.JockeyProfiles
                .Where(p => jockeyIds.Contains(p.UserId))
                .ToListAsync(cancellationToken);

            foreach (var res in results)
            {
                if (!jockeyByEntry.TryGetValue(res.EntryId, out var jockeyId)) continue;
                var profile = profiles.FirstOrDefault(p => p.UserId == jockeyId);
                if (profile is null) continue;

                profile.TotalRaces = Math.Max(0, profile.TotalRaces - 1);
                if (!res.IsRaceDQ && res.FinalPosition == 1)
                    profile.TotalWins = Math.Max(0, profile.TotalWins - 1);
                if (!res.IsRaceDQ && res.FinalPosition is >= 1 and <= 3)
                    profile.TotalTop3 = Math.Max(0, profile.TotalTop3 - 1);
                var prize = res.IsRaceDQ
                    ? 0
                    : RaceExecutionConstants.PrizePointsFor(res.FinalPosition ?? 0, fieldSize);
                profile.CareerPrizePoints = Math.Max(0, profile.CareerPrizePoints - prize);
                profile.UpdatedAt = now;
            }

            _context.RaceResults.RemoveRange(results);

            // ── 5. Đưa race về PendingResult ──
            race.Status = RaceExecutionConstants.RacePendingResult;
            race.PublishedAt = null;
            race.UpdatedAt = now;

            await _reviewHistoryRepository.AddAsync(
                new ReviewHistory
                {
                    EntityType = ReviewEntity.Race,
                    EntityId = race.RaceId,
                    Action = ReviewAction.Unpublished,
                    Reason = reason,
                    BeforeData = beforeSnapshot,
                    AfterData = ReviewHistoryJson.Serialize(new
                    {
                        raceId = race.RaceId,
                        name = race.Name,
                        status = race.Status,
                        publishedAt = race.PublishedAt,
                        reversedPayouts = reversed,
                        rolledBackSettlementRuns = runs.Count
                    }),
                    AdminId = request.AdminUserId
                },
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            // Rollback về PendingResult → đẩy để spectator thấy đúng trạng thái.
            _liveTracker.MarkChanged(race.RaceId);

            return new UnpublishRaceResultResponse(race.RaceId, race.Status, reversed);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
