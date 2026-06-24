using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PredictionSettlements.CreatePredictionSettlement;

public sealed class CreatePredictionSettlementCommandHandler
    : IRequestHandler<CreatePredictionSettlementCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreatePredictionSettlementCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreatePredictionSettlementCommand request,
        CancellationToken cancellationToken)
    {
        if (request.SettlementRunId <= 0)
            throw new InvalidOperationException("SettlementRunId is invalid.");

        if (request.PredictionId <= 0)
            throw new InvalidOperationException("PredictionId is invalid.");

        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is invalid.");

        if (request.SpectatorId <= 0)
            throw new InvalidOperationException("SpectatorId is invalid.");

        if (string.IsNullOrWhiteSpace(request.Outcome))
            throw new InvalidOperationException("Outcome is required.");

        if (request.MatchedCount < 0)
            throw new InvalidOperationException("MatchedCount cannot be negative.");

        if (request.BetAmount < 0)
            throw new InvalidOperationException("BetAmount cannot be negative.");

        if (request.OddsAverage <= 0)
            throw new InvalidOperationException("OddsAverage must be greater than zero.");

        if (request.PayoutAmount < 0)
            throw new InvalidOperationException("PayoutAmount cannot be negative.");

        var settlementRunExists = await _context.SettlementRuns
            .AnyAsync(x => x.SettlementRunId == request.SettlementRunId, cancellationToken);

        if (!settlementRunExists)
            throw new InvalidOperationException("SettlementRun not found.");

        var predictionExists = await _context.Predictions
            .AnyAsync(x => x.PredictionId == request.PredictionId, cancellationToken);

        if (!predictionExists)
            throw new InvalidOperationException("Prediction not found.");

        var raceExists = await _context.Races
            .AnyAsync(x => x.RaceId == request.RaceId, cancellationToken);

        if (!raceExists)
            throw new InvalidOperationException("Race not found.");

        var spectatorExists = await _context.Spectators
            .AnyAsync(x => x.UserId == request.SpectatorId, cancellationToken);

        if (!spectatorExists)
            throw new InvalidOperationException("Spectator not found.");

        if (request.PayoutTransactionId.HasValue)
        {
            var transactionExists = await _context.WalletTransactions
                .AnyAsync(x => x.WalletTransactionId == request.PayoutTransactionId.Value, cancellationToken);

            if (!transactionExists)
                throw new InvalidOperationException("WalletTransaction not found.");
        }

        var entity = new PredictionSettlement
        {
            SettlementRunId = request.SettlementRunId,
            PredictionId = request.PredictionId,
            RaceId = request.RaceId,
            SpectatorId = request.SpectatorId,
            MatchedCount = request.MatchedCount,
            Outcome = request.Outcome.Trim(),
            BetAmount = request.BetAmount,
            OddsAverage = request.OddsAverage,
            PayoutAmount = request.PayoutAmount,
            NetAmount = request.NetAmount,
            PayoutTransactionId = request.PayoutTransactionId,
            IsRollbacked = false,
            SettledAt = DateTime.UtcNow
        };

        _context.PredictionSettlements.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.PredictionSettlementId;
    }
}