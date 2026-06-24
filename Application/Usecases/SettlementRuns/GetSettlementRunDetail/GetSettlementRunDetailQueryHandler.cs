using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.SettlementRuns.GetSettlementRunDetail;

public sealed class GetSettlementRunDetailQueryHandler
    : IRequestHandler<GetSettlementRunDetailQuery, SettlementRunDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetSettlementRunDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SettlementRunDetailResponse?> Handle(
        GetSettlementRunDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.SettlementRuns
            .AsNoTracking()
            .Where(x => x.SettlementRunId == request.SettlementRunId)
            .Select(x => new SettlementRunDetailResponse(
                x.SettlementRunId,
                x.RaceId,
                x.Type,
                x.Status,
                x.TotalPredictions,
                x.TotalBetAmount,
                x.TotalPayoutAmount,
                x.TriggeredByAdminId
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}