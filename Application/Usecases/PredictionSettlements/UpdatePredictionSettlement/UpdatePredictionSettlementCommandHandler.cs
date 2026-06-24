using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PredictionSettlements.UpdatePredictionSettlement;

public sealed class UpdatePredictionSettlementCommandHandler
    : IRequestHandler<UpdatePredictionSettlementCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdatePredictionSettlementCommandHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdatePredictionSettlementCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PredictionSettlementId <= 0)
            throw new InvalidOperationException("PredictionSettlementId is invalid.");

        if (request.SettlementRunId <= 0)
            throw new InvalidOperationException("SettlementRunId is invalid.");

        if (request.PredictionId <= 0)
            throw new InvalidOperationException("PredictionId is invalid.");

        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is invalid.");

        if (request.SpectatorId <= 0)
            throw new InvalidOperationException("SpectatorId is invalid.");

        if (request.MatchedCount < 0)
            throw new InvalidOperationException("MatchedCount cannot be negative.");

        if (request.BetAmount < 0)
            throw new InvalidOperationException("BetAmount cannot be negative.");

        if (request.OddsAverage < 0)
            throw new InvalidOperationException("OddsAverage cannot be negative.");

        if (request.PayoutAmount < 0)
            throw new InvalidOperationException("PayoutAmount cannot be negative.");

        if (string.IsNullOrWhiteSpace(request.Outcome))
            throw new InvalidOperationException("Outcome is required.");

        var entity = await _context.PredictionSettlements
            .FirstOrDefaultAsync(
                x => x.PredictionSettlementId == request.PredictionSettlementId,
                cancellationToken);

        if (entity is null)
            return false;

        var settlementRunExists = await _context.SettlementRuns
            .AnyAsync(x => x.SettlementRunId == request.SettlementRunId,
                cancellationToken);

        if (!settlementRunExists)
            throw new InvalidOperationException("SettlementRun does not exist.");

        var predictionExists = await _context.Predictions
            .AnyAsync(x => x.PredictionId == request.PredictionId,
                cancellationToken);

        if (!predictionExists)
            throw new InvalidOperationException("Prediction does not exist.");

        var raceExists = await _context.Races
            .AnyAsync(x => x.RaceId == request.RaceId,
                cancellationToken);

        if (!raceExists)
            throw new InvalidOperationException("Race does not exist.");

        var spectatorExists = await _context.Spectators
            .AnyAsync(x => x.UserId == request.SpectatorId,
                cancellationToken);

        if (!spectatorExists)
            throw new InvalidOperationException("Spectator does not exist.");

        entity.SettlementRunId = request.SettlementRunId;
        entity.PredictionId = request.PredictionId;
        entity.RaceId = request.RaceId;
        entity.SpectatorId = request.SpectatorId;
        entity.MatchedCount = request.MatchedCount;
        entity.Outcome = request.Outcome.Trim();
        entity.BetAmount = request.BetAmount;
        entity.OddsAverage = request.OddsAverage;
        entity.PayoutAmount = request.PayoutAmount;
        entity.NetAmount = request.NetAmount;
        entity.IsRollbacked = request.IsRollbacked;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}