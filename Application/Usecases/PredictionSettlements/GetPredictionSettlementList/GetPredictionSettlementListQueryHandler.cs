using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PredictionSettlements.GetPredictionSettlementList;

public sealed class GetPredictionSettlementListQueryHandler
    : IRequestHandler<GetPredictionSettlementListQuery, List<PredictionSettlementListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetPredictionSettlementListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PredictionSettlementListItemResponse>> Handle(
        GetPredictionSettlementListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.PredictionSettlements
            .AsNoTracking()
            .OrderByDescending(x => x.SettledAt)
            .Select(x => new PredictionSettlementListItemResponse(
                x.PredictionSettlementId,
                x.PredictionId,
                x.Outcome,
                x.PayoutAmount,
                x.IsRollbacked
            ))
            .ToListAsync(cancellationToken);
    }
}