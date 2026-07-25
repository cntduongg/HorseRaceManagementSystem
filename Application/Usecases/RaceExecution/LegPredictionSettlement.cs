using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

/// <summary>
/// Quyết toán dự đoán per-leg khi leg đã có kết quả chính thức (1st).
/// Lưới an toàn: settle cả Pending và Locked (Pending → Locked trước khi payout).
/// </summary>
public static class LegPredictionSettlement
{
    public static async Task SettleAsync(
        IApplicationDbContext context,
        int raceId,
        int legNumber,
        DateTime now,
        CancellationToken ct)
    {
        var winnerResult = await context.LegOfficialResults
            .FirstOrDefaultAsync(r => r.RaceId == raceId && r.LegNumber == legNumber && r.FinishPosition == 1, ct);

        if (winnerResult == null) return;

        var run = new SettlementRun
        {
            RaceId = raceId,
            Type = $"Leg{legNumber}Publish",
            Status = "Completed",
            CreatedAt = now
        };
        context.SettlementRuns.Add(run);
        await context.SaveChangesAsync(ct);

        var predictions = await context.Predictions
            .Where(p => p.RaceId == raceId && p.LegNumber == legNumber &&
                        (p.Status == PredictionStatus.Locked || p.Status == PredictionStatus.Pending))
            .ToListAsync(ct);

        foreach (var p in predictions)
        {
            if (p.Status == PredictionStatus.Pending)
                p.Status = PredictionStatus.Locked;
        }

        var spectatorIds = predictions.Select(p => p.SpectatorId).Distinct().ToList();
        var wallets = await context.PointWallets.Where(w => spectatorIds.Contains(w.SpectatorId)).ToListAsync(ct);

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
                    context.WalletTransactions.Add(payoutTx);
                    await context.SaveChangesAsync(ct);
                    payoutTxId = payoutTx.WalletTransactionId;
                }
            }

            context.PredictionSettlements.Add(new PredictionSettlement
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
}
