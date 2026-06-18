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
            throw new InvalidOperationException("SettlementRunId invalid.");

        if (request.PredictionId <= 0)
            throw new InvalidOperationException("PredictionId invalid.");

        if (string.IsNullOrWhiteSpace(request.Outcome))
            throw new InvalidOperationException("Outcome is required.");

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