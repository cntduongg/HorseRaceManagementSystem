using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// GET /api/races/{raceId}/execution — trạng thái vận hành đầy đủ (mọi role xem được).
public sealed record GetRaceExecutionQuery(int RaceId, int CurrentUserId)
    : IQuery<RaceExecutionResponse>;

public sealed record LegResultDto(int EntryId, int Position, int Points);

public sealed record LegStatusDto(
    int LegIndex,
    int LegNumber,
    string Status,
    bool Referee1Submitted,
    bool Referee2Submitted,
    bool MySubmitted,
    IReadOnlyList<LegResultDto> Results);

public sealed record RaceExecutionResponse(
    int RaceId,
    string Status,
    bool IsBetsLocked,
    int TotalLegs,
    int CurrentLegIndex,
    IReadOnlyList<LegStatusDto> Legs);

public sealed class GetRaceExecutionQueryHandler
    : IRequestHandler<GetRaceExecutionQuery, RaceExecutionResponse>
{
    private readonly IApplicationDbContext _context;

    public GetRaceExecutionQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RaceExecutionResponse> Handle(
        GetRaceExecutionQuery request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .AsNoTracking()
            .Include(r => r.Legs).ThenInclude(l => l.RefereeEntries)
            .Include(r => r.Legs).ThenInclude(l => l.OfficialResults)
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        var legs = race.Legs
            .OrderBy(l => l.LegNumber)
            .Select(l => new LegStatusDto(
                LegIndex: l.LegNumber - 1,
                LegNumber: l.LegNumber,
                Status: l.Status,
                Referee1Submitted: race.Referee1Id != null &&
                    l.RefereeEntries.Any(e => e.RefereeUserId == race.Referee1Id),
                Referee2Submitted: race.Referee2Id != null &&
                    l.RefereeEntries.Any(e => e.RefereeUserId == race.Referee2Id),
                MySubmitted: request.CurrentUserId > 0 &&
                    l.RefereeEntries.Any(e => e.RefereeUserId == request.CurrentUserId),
                Results: l.OfficialResults
                    .Select(o => new LegResultDto(
                        o.EntryId,
                        RaceExecutionConstants.EncodePosition(o.FinishPosition, o.ResultStatus),
                        o.LegPoints))
                    .ToList()))
            .ToList();

        var current = legs.FindIndex(l =>
            l.Status != RaceExecutionConstants.LegConfirmed &&
            l.Status != RaceExecutionConstants.LegResolved);
        if (current < 0) current = Math.Max(0, legs.Count - 1);

        var isBetsLocked = race.Status != RaceExecutionConstants.RaceScheduled;

        return new RaceExecutionResponse(
            race.RaceId,
            race.Status,
            isBetsLocked,
            race.NumberOfLegs,
            current,
            legs);
    }
}
