using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.PlacePrediction;

public sealed class PlacePredictionCommandHandler
    : IRequestHandler<PlacePredictionCommand, PlacePredictionResponse>
{
    private readonly IApplicationDbContext _context;

    public PlacePredictionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlacePredictionResponse> Handle(
        PlacePredictionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BetAmount < 10)
        {
            throw new InvalidOperationException("Bet amount must be at least 10 points.");
        }

        var race = await _context.Races
            .FirstOrDefaultAsync(x => x.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        if (!string.Equals(
                race.Status?.Trim(),
                RaceExecutionConstants.RaceScheduled,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Only scheduled races can accept predictions. Current status: {race.Status}");
        }

        if (race.OddsComputedAt is null)
        {
            throw new InvalidOperationException(
                "Race odds are not locked yet. Admin must close registration first.");
        }

        var spectator = await _context.Spectators
            .FirstOrDefaultAsync(x => x.UserId == request.SpectatorId, cancellationToken)
            ?? throw new InvalidOperationException("Current user is not a spectator.");

        var wallet = await _context.PointWallets
            .FirstOrDefaultAsync(x => x.SpectatorId == spectator.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Spectator wallet not found.");

        if (wallet.IsFrozen)
        {
            throw new InvalidOperationException("Wallet is frozen.");
        }

        var maxBet = wallet.Balance * 0.5m;

        if (request.BetAmount > maxBet)
        {
            throw new InvalidOperationException(
                $"Bet amount cannot exceed 50% of wallet balance. Max allowed: {maxBet}.");
        }

        if (wallet.Balance < request.BetAmount)
        {
            throw new InvalidOperationException("Insufficient wallet balance.");
        }

        var alreadyHasActivePrediction = await _context.Predictions
            .AnyAsync(x =>
                    x.RaceId == request.RaceId &&
                    x.SpectatorId == spectator.UserId &&
                    x.Status != PredictionStatus.Cancelled,
                cancellationToken);

        if (alreadyHasActivePrediction)
        {
            throw new InvalidOperationException("Only one prediction is allowed per race.");
        }

        var entry = await _context.Entries
            .FirstOrDefaultAsync(
                x => x.EntryId == request.EntryId &&
                     x.RaceId == request.RaceId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Entry not found in this race.");

        if (!string.Equals(
                entry.Status?.Trim(),
                RaceExecutionConstants.EntryApproved,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Only approved entries can be predicted. Current entry status: {entry.Status}");
        }

        if (entry.Odds <= 0)
        {
            throw new InvalidOperationException("Entry does not have valid locked odds.");
        }

        var now = DateTime.UtcNow;

        var prediction = new Prediction
        {
            RaceId = race.RaceId,
            SpectatorId = spectator.UserId,
            FirstEntryId = entry.EntryId,
            SecondEntryId = null,
            ThirdEntryId = null,
            BetAmount = request.BetAmount,
            OddsLocked1 = entry.Odds,
            OddsLocked2 = null,
            OddsLocked3 = null,
            Status = PredictionStatus.Pending,
            CreatedAt = now
        };

        wallet.Balance -= request.BetAmount;
        wallet.UpdatedAt = now;

        _context.Predictions.Add(prediction);

        await _context.SaveChangesAsync(cancellationToken);

        return new PlacePredictionResponse(
            prediction.PredictionId,
            prediction.RaceId,
            prediction.SpectatorId,
            prediction.FirstEntryId,
            prediction.BetAmount,
            prediction.OddsLocked1,
            prediction.Status,
            prediction.CreatedAt);
    }
}