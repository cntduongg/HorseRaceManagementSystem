using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Legs.GetLegList;

public sealed class GetLegListQueryHandler
    : IRequestHandler<GetLegListQuery, List<LegListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetLegListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LegListItemResponse>> Handle(
        GetLegListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Legs
            .Select(x => new LegListItemResponse(
                x.RaceId,
                x.LegNumber,
                x.Status
            ))
            .ToListAsync(cancellationToken);
    }
}