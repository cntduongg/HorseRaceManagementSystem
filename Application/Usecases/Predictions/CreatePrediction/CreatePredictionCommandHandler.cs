using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Application.Usecases.Predictions.Common;

namespace Application.Usecases.Predictions.CreatePrediction;

public sealed class CreatePredictionCommandHandler : IRequestHandler<CreatePredictionCommand, int>
{
    private const decimal MinBet = 10m;
    private readonly IApplicationDbContext _context;
    public CreatePredictionCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<int> Handle(CreatePredictionCommand request, CancellationToken cancellationToken)
    {
        if (request.BetAmount < MinBet) throw new InvalidOperationException($"Min bet is {MinBet} points.");

        var leg = await _context.Legs
            .FirstOrDefaultAsync(l => l.RaceId == request.RaceId && l.LegNumber == request.LegNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Leg not found.");

        // CHỈ KHÓA CƯỢC KHI LEG ĐÓ ĐANG DIỄN RA HOẶC ĐÃ XONG
        if (leg.ExecutionStatus == "InProgress" || leg.ExecutionStatus == "Completed" || leg.ExecutionStatus == "Cancelled")
            throw new InvalidOperationException($"Betting is locked because the leg is {leg.ExecutionStatus}.");

        var spectator = await _context.Spectators.FirstOrDefaultAsync(x => x.UserId == request.SpectatorId, cancellationToken)
            ?? throw new KeyNotFoundException("Spectator not found.");
        if (!spectator.IsActive) throw new InvalidOperationException("Spectator account is locked.");

        // Mỗi spectator chỉ được cược tối đa 1 lần/Leg
        var hasActive = await _context.Predictions.AnyAsync(
            p => p.RaceId == request.RaceId && p.LegNumber == request.LegNumber && p.SpectatorId == request.SpectatorId && p.Status != PredictionStatus.Cancelled,
            cancellationToken);
        if (hasActive) throw new InvalidOperationException("You already have an active prediction for this leg.");

        var wallet = await _context.PointWallets.FirstOrDefaultAsync(w => w.SpectatorId == request.SpectatorId, cancellationToken)
            ?? throw new KeyNotFoundException("Wallet not found.");
        if (wallet.Balance < request.BetAmount) throw new InvalidOperationException("Insufficient balance.");
        if (request.BetAmount > wallet.Balance * 0.5m) throw new InvalidOperationException("Cannot bet more than 50% of your balance.");

        var odds = await PredictionOddsCalculator.CalculateEntryLegOddsAsync(_context, request.RaceId, request.LegNumber, request.FirstEntryId, request.BetAmount, cancellationToken);
        var now = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var prediction = new Prediction
            {
                RaceId = request.RaceId,
                LegNumber = request.LegNumber,
                SpectatorId = request.SpectatorId,
                FirstEntryId = request.FirstEntryId,
                BetAmount = request.BetAmount,
                OddsLocked1 = odds,
                Status = PredictionStatus.Pending,
                CreatedAt = now
            };

            _context.Predictions.Add(prediction);
            await _context.SaveChangesAsync(cancellationToken);

            wallet.Balance -= request.BetAmount;
            wallet.UpdatedAt = now;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.WalletId,
                SpectatorId = request.SpectatorId,
                PredictionId = prediction.PredictionId,
                Type = "BetPlaced",
                Amount = -request.BetAmount,
                BalanceAfter = wallet.Balance,
                Reason = $"Bet on Race #{request.RaceId} Leg #{request.LegNumber}",
                CreatedAt = now
            });

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return prediction.PredictionId;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}