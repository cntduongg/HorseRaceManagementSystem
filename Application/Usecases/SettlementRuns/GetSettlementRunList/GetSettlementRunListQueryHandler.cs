using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.SettlementRuns.GetSettlementRunList;

public sealed class GetSettlementRunListQueryHandler
    : IRequestHandler<GetSettlementRunListQuery, List<SettlementRunListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetSettlementRunListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SettlementRunListItemResponse>> Handle(
        GetSettlementRunListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.SettlementRuns
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new SettlementRunListItemResponse(
                x.SettlementRunId,
                x.RaceId,
                x.Type,
                x.Status
            ))
            .ToListAsync(cancellationToken);
    }
}