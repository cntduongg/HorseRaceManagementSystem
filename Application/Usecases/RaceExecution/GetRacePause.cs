using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// GET /api/races/{raceId}/pause — thông tin leg để so sánh 2 trọng tài.
//  - ADMIN: xem leg đang Conflicted (mặc định, legNumber=null) hoặc bất kỳ leg nào (legNumber cụ thể).
//  - REFEREE: chỉ xem được leg đã Resolved (legNumber bắt buộc), và chỉ với race mình được phân công —
//    tránh lộ dữ liệu 2 trọng tài lúc đang xử lý conflict (giữ Blind Double-Entry).
public sealed record GetRacePauseQuery(
    int RaceId,
    int? LegNumber,
    int CurrentUserId,
    bool IsAdmin) : IQuery<RacePauseResponse?>;

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

        if (!request.IsAdmin)
        {
            var isAssignedReferee =
                request.CurrentUserId == race.Referee1Id ||
                request.CurrentUserId == race.Referee2Id;

            if (!isAssignedReferee)
                throw new UnauthorizedAccessException(
                    "Only an assigned referee can view this race's pause data.");

            if (request.LegNumber is null)
                throw new UnauthorizedAccessException(
                    "A legNumber is required to view a resolved leg's comparison.");
        }

        Domain.Aggregates.Entities.Leg? targetLeg;

        if (request.LegNumber is { } requestedLegNumber)
        {
            // Xem lại 1 leg cụ thể (VD đã Resolved) — không giới hạn Status cho Admin.
            targetLeg = race.Legs
                .FirstOrDefault(l => l.LegNumber == requestedLegNumber);

            if (targetLeg is null)
                throw new KeyNotFoundException("Leg not found.");

            // Referee: chỉ được xem leg đã Resolved — chưa Resolved thì vẫn đang trong
            // giai đoạn Blind Double-Entry / chờ Admin xử lý, không được lộ ra.
            if (!request.IsAdmin && targetLeg.Status != RaceExecutionConstants.LegResolved)
                throw new UnauthorizedAccessException(
                    "This leg has not been resolved yet.");
        }
        else
        {
            // Hành vi cũ (chỉ Admin gọi tới nhánh này): tự tìm leg đang Conflicted.
            targetLeg = race.Legs
                .Where(l => l.Status == RaceExecutionConstants.LegConflicted)
                .OrderBy(l => l.LegNumber)
                .FirstOrDefault();

            if (targetLeg is null)
                return new RacePauseResponse(race.RaceId, race.Status, null);
        }

        var legNumber = targetLeg.LegNumber;

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