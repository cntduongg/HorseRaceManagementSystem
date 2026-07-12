using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.DeletePrediction;


public sealed class DeletePredictionCommandHandler
    : IRequestHandler<DeletePredictionCommand, bool>
{
    private const string RaceScheduled = "Scheduled";

    private readonly IApplicationDbContext _context;

    public DeletePredictionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeletePredictionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PredictionId <= 0)
            throw new InvalidOperationException("PredictionId is required.");

        if (request.SpectatorId <= 0)
            throw new InvalidOperationException("SpectatorId is required.");

        var prediction = await _context.Predictions
            .Include(x => x.Race)
            .FirstOrDefaultAsync(
                x => x.PredictionId == request.PredictionId &&
                     x.SpectatorId == request.SpectatorId,
                cancellationToken)
            ?? throw new InvalidOperationException("Prediction not found.");

        if (prediction.Status == PredictionStatus.Cancelled)
            throw new InvalidOperationException("Prediction already cancelled.");

        if (prediction.Status != PredictionStatus.Pending)
            throw new InvalidOperationException(
                $"Only pending prediction can be cancelled. Current status: {prediction.Status}");

        if (prediction.Race is null)
            throw new InvalidOperationException("Prediction race not found.");

        if (!string.Equals(
                prediction.Race.Status?.Trim(),
                RaceScheduled,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"A prediction can only be cancelled while the race is Scheduled. Current race status: {prediction.Race.Status}");
        }

        var wallet = await _context.PointWallets
            .FirstOrDefaultAsync(
                x => x.SpectatorId == request.SpectatorId,
                cancellationToken)
            ?? throw new InvalidOperationException("Point wallet not found.");

        if (wallet.IsFrozen)
            throw new InvalidOperationException("The point wallet is frozen.");

        var now = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            prediction.Status = PredictionStatus.Cancelled;
            prediction.CancelledAt = now;

            wallet.Balance += prediction.BetAmount;
            wallet.UpdatedAt = now;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.WalletId,
                SpectatorId = request.SpectatorId,
                PredictionId = prediction.PredictionId,
                Type = "BetRefunded",
                Amount = prediction.BetAmount,
                BalanceAfter = wallet.Balance,
                Reason = $"Refund for cancelled prediction #{prediction.PredictionId}",
                CreatedAt = now
            });

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