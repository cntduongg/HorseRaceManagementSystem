using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Application.Usecases.Predictions.Common;

namespace Application.Usecases.Predictions.CreatePrediction;

public sealed class CreatePredictionCommandHandler
    : IRequestHandler<CreatePredictionCommand, int>
{
    private const decimal MinBet = 10m;
    private const string RaceScheduled = "Scheduled";
    private const string EntryApproved = "Approved";

    private readonly IApplicationDbContext _context;

    public CreatePredictionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreatePredictionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is required.");

        if (request.SpectatorId <= 0)
            throw new InvalidOperationException("SpectatorId is required.");

        if (request.FirstEntryId <= 0)
            throw new InvalidOperationException("EntryId is required.");

        if (request.BetAmount < MinBet)
            throw new InvalidOperationException($"The minimum bet is {MinBet} points.");

        var race = await _context.Races
            .FirstOrDefaultAsync(x => x.RaceId == request.RaceId, cancellationToken)
            ?? throw new InvalidOperationException("Race not found.");

        if (!string.Equals(race.Status?.Trim(), RaceScheduled, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"You can only place a bet while the race is Scheduled. Current status: {race.Status}");
        }

        if (race.OddsComputedAt is null)
        {
            throw new InvalidOperationException(
                "Odds have not been locked. Admin must close registration before spectators can place a prediction.");
        }

        var spectator = await _context.Spectators
            .FirstOrDefaultAsync(x => x.UserId == request.SpectatorId, cancellationToken)
            ?? throw new InvalidOperationException("Spectator not found.");

        if (!spectator.IsActive)
            throw new InvalidOperationException("The spectator account is locked.");

        var entry = await _context.Entries
            .FirstOrDefaultAsync(
                e => e.EntryId == request.FirstEntryId &&
                     e.RaceId == request.RaceId,
                cancellationToken)
            ?? throw new InvalidOperationException("The entry does not belong to the selected race.");

        if (!string.Equals(entry.Status?.Trim(), EntryApproved, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The entry has not been approved. Current status: {entry.Status}");
        }

        if (entry.Odds <= 0)
        {
            throw new InvalidOperationException(
                "The entry does not have valid locked odds.");
        }

        var hasActive = await _context.Predictions.AnyAsync(
            p => p.RaceId == request.RaceId &&
                 p.SpectatorId == request.SpectatorId &&
                 p.Status != PredictionStatus.Cancelled,
            cancellationToken);

        if (hasActive)
            throw new InvalidOperationException("You already have an active prediction for this race.");

        var wallet = await _context.PointWallets
            .FirstOrDefaultAsync(w => w.SpectatorId == request.SpectatorId, cancellationToken)
            ?? throw new InvalidOperationException("Point wallet not found.");

        if (wallet.IsFrozen)
            throw new InvalidOperationException("The point wallet is frozen.");

        if (wallet.Balance < request.BetAmount)
            throw new InvalidOperationException("Insufficient balance.");

        var maxBet = wallet.Balance * 0.5m;

        if (request.BetAmount > maxBet)
            throw new InvalidOperationException("The bet amount cannot exceed 50% of the balance.");

        var odds = await PredictionOddsCalculator.CalculateEntryOddsAsync(
            _context,
            request.RaceId,
            request.FirstEntryId,
            request.BetAmount,
            cancellationToken);

        var now = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var prediction = new Prediction
            {
                RaceId = request.RaceId,
                SpectatorId = request.SpectatorId,
                FirstEntryId = request.FirstEntryId,
                SecondEntryId = null,
                ThirdEntryId = null,

                BetAmount = request.BetAmount,

                OddsLocked1 = odds,
                OddsLocked2 = null,
                OddsLocked3 = null,

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
                Reason = $"Bet placed on race #{request.RaceId}, entry #{request.FirstEntryId}, odds {odds}",
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