using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PredictionSettlements.UpdatePredictionSettlement;

public sealed class UpdatePredictionSettlementCommandHandler
    : IRequestHandler<UpdatePredictionSettlementCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdatePredictionSettlementCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdatePredictionSettlementCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Outcome))
            throw new InvalidOperationException("Outcome is required.");

        var entity = await _context.PredictionSettlements
            .FirstOrDefaultAsync(x =>
                x.PredictionSettlementId == request.PredictionSettlementId,
                cancellationToken);

        if (entity is null)
            return false;

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