using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// GET /api/races/{raceId}/pause — thông tin leg đang Conflicted để Admin so sánh song song.
public sealed record GetRacePauseQuery(int RaceId) : IQuery<RacePauseResponse?>;

public sealed record ConflictComparisonItem(
    int EntryId,
    int? GateNumber,
    string HorseName,
    int? Referee1Position,
    int? Referee2Position);

public sealed record ConflictedLegDto(
    int LegIndex,
    int LegNumber,
    IReadOnlyList<ConflictComparisonItem> Comparison);

public sealed record RacePauseResponse(
    int RaceId,
    string RaceStatus,
    ConflictedLegDto? ConflictedLeg);

public sealed class GetRacePauseQueryHandler
    : IRequestHandler<GetRacePauseQuery, RacePauseResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetRacePauseQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RacePauseResponse?> Handle(
        GetRacePauseQuery request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .AsNoTracking()
            .Include(r => r.Legs)
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        var conflicted = race.Legs
            .Where(l => l.Status == RaceExecutionConstants.LegConflicted)
            .OrderBy(l => l.LegNumber)
            .FirstOrDefault();

        if (conflicted is null)
            return new RacePauseResponse(race.RaceId, race.Status, null);

        var legNumber = conflicted.LegNumber;

        var entries = await _context.Entries
            .AsNoTracking()
            .Include(e => e.Horse)
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .OrderBy(e => e.GateNumber)
            .ToListAsync(cancellationToken);

        var refEntries = await _context.LegRefereeEntries
            .AsNoTracking()
            .Where(x => x.RaceId == request.RaceId && x.LegNumber == legNumber)
            .ToListAsync(cancellationToken);

        int? PositionFor(int entryId, int? refereeId)
        {
            if (refereeId is null) return null;
            var row = refEntries.FirstOrDefault(
                x => x.EntryId == entryId && x.RefereeUserId == refereeId);
            return row is null
                ? null
                : RaceExecutionConstants.EncodePosition(row.FinishPosition, row.ResultStatus);
        }

        var comparison = entries.Select(e => new ConflictComparisonItem(
                e.EntryId,
                e.GateNumber,
                e.Horse?.Name ?? $"Horse #{e.HorseId}",
                PositionFor(e.EntryId, race.Referee1Id),
                PositionFor(e.EntryId, race.Referee2Id)))
            .ToList();

        return new RacePauseResponse(
            race.RaceId,
            race.Status,
            new ConflictedLegDto(legNumber - 1, legNumber, comparison));
    }
}
