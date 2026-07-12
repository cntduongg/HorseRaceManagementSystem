using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Races.GetRaceDetail;

public sealed class GetRaceDetailQueryHandler
    : IRequestHandler<GetRaceDetailQuery, RaceDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetRaceDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RaceDetailResponse?> Handle(
        GetRaceDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Races
            .Where(x => x.RaceId == request.RaceId)
            .Select(x => new RaceDetailResponse(
                x.RaceId,
                x.TournamentId,
                x.Name,
                x.ScheduledStartTime,
                x.ScheduledEndTime,
                x.NumberOfLegs,
                x.MaxHorses,
                x.RoundType,
                x.Status,
                x.Referee1Id,
                x.Referee2Id
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}