using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.DeletePrediction;

// Flow 7 — Hủy dự đoán: chỉ khi race còn Scheduled, hoàn 100% tiền cược vào ví (giữ audit).
public sealed class DeletePredictionCommandHandler
    : IRequestHandler<DeletePredictionCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeletePredictionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeletePredictionCommand request,
        CancellationToken cancellationToken)
    {
        var prediction = await _context.Predictions
            .FirstOrDefaultAsync(x => x.PredictionId == request.PredictionId, cancellationToken);

        if (prediction is null)
            return false;

        if (prediction.Status != "Pending")
            throw new InvalidOperationException("Chỉ hủy được dự đoán đang hoạt động.");

        var race = await _context.Races
            .FirstOrDefaultAsync(r => r.RaceId == prediction.RaceId, cancellationToken)
            ?? throw new InvalidOperationException("Race not found.");

        if (race.Status != "Scheduled")
            throw new InvalidOperationException("Chỉ hủy được cược khi cuộc đua còn Scheduled.");

        var now = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Hoàn 100% vào ví.
            var wallet = await _context.PointWallets
                .FirstOrDefaultAsync(w => w.SpectatorId == prediction.SpectatorId, cancellationToken);

            if (wallet is { IsFrozen: false })
            {
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
                    Reason = $"Hoàn cược race #{prediction.RaceId}",
                    CreatedAt = now
                });
            }

            prediction.Status = "Cancelled";
            prediction.CancelledAt = now;

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
