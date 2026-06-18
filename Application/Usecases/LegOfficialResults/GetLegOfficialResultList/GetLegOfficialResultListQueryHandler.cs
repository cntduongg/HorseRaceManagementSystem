using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.LegOfficialResults.GetLegOfficialResultList;

public sealed class GetLegOfficialResultListQueryHandler
    : IRequestHandler<GetLegOfficialResultListQuery, List<LegOfficialResultListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetLegOfficialResultListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LegOfficialResultListItemResponse>> Handle(
        GetLegOfficialResultListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.LegOfficialResults
            .AsNoTracking()
            .Select(x => new LegOfficialResultListItemResponse(
                x.RaceId,
                x.LegNumber,
                x.EntryId,
                x.FinishPosition,
                x.ResultStatus
            ))
            .ToListAsync(cancellationToken);
    }
}