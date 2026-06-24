using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PredictionSettlements.GetPredictionSettlementDetail;

public sealed class GetPredictionSettlementDetailQueryHandler
    : IRequestHandler<GetPredictionSettlementDetailQuery, PredictionSettlementDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetPredictionSettlementDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PredictionSettlementDetailResponse?> Handle(
        GetPredictionSettlementDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.PredictionSettlements
            .AsNoTracking()
            .Where(x => x.PredictionSettlementId == request.PredictionSettlementId)
            .Select(x => new PredictionSettlementDetailResponse(
                x.PredictionSettlementId,
                x.SettlementRunId,
                x.PredictionId,
                x.RaceId,
                x.SpectatorId,
                x.MatchedCount,
                x.Outcome,
                x.BetAmount,
                x.OddsAverage,
                x.PayoutAmount,
                x.NetAmount,
                x.IsRollbacked
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}