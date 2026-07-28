using Application.Common;
using Application.Usecases.Races.GetRaceList;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class RaceReadService : IRaceReadService
{
    private readonly ApplicationDbContext _db;

    public RaceReadService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<RaceListItemResponse>> GetListAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Races
            .AsNoTracking()
            .OrderByDescending(x => x.ScheduledStartTime)
            .Select(x => new RaceListItemResponse(
                x.RaceId,
                x.TournamentId,
                x.Tournament.Name,
                x.Name,
                x.ScheduledStartTime,
                x.ScheduledEndTime,
                x.NumberOfLegs,
                x.MaxHorses,
                x.RoundType,
                x.Status,
                x.Referee1Id,
                x.Referee1 != null ? x.Referee1.FullName : null,
                x.Referee1 != null ? x.Referee1.AvatarUrl : null,
                x.Referee2Id,
                x.Referee2 != null ? x.Referee2.FullName : null,
                x.Referee2 != null ? x.Referee2.AvatarUrl : null,
                x.RegistrationOpenAt,
                x.RegistrationCloseAt,
                x.OddsComputedAt,
                x.OddsPublishedAt,
                x.BettingLockedAt,
                x.PublishedAt
            ))
            .ToListAsync(cancellationToken);
    }
}