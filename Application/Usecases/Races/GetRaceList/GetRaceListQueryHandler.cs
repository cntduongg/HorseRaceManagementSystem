using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Races.GetRaceList;

public sealed class GetRaceListQueryHandler
    : IRequestHandler<GetRaceListQuery, List<RaceListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetRaceListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RaceListItemResponse>> Handle(
        GetRaceListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Races
            .Select(x => new RaceListItemResponse(
                x.RaceId,
                x.Name,
                x.ScheduledStartTime,
                x.Status
            ))
            .ToListAsync(cancellationToken);
    }
}