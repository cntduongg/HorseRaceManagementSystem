using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// GET /api/races/{raceId}/legs/{legIndex}/referee-view
// Trả dữ liệu leg cho referee đang đăng nhập; ẩn input của referee kia đến khi cả hai submit.
public sealed record GetRefereeLegViewQuery(int RaceId, int LegIndex, int CurrentUserId)
    : IQuery<RefereeLegViewResponse>;

public sealed record RefereeLegEntryDto(
    int EntryId,
    int? GateNumber,
    int HorseId,
    string HorseName,
    string JockeyName);

public sealed record RefereeSubmittedItemDto(int EntryId, int Position);

public sealed record RefereeLegViewResponse(
    int RaceId,
    int LegIndex,
    int LegNumber,
    IReadOnlyList<RefereeLegEntryDto> Entries,
    IReadOnlyList<RefereeSubmittedItemDto>? MySubmittedData,
    IReadOnlyList<RefereeSubmittedItemDto>? MyDraftData,
    bool MySubmitted,
    bool OpponentSubmitted,
    bool BothSubmitted,
    string? LegStatus);

public sealed class GetRefereeLegViewQueryHandler
    : IRequestHandler<GetRefereeLegViewQuery, RefereeLegViewResponse>
{
    private readonly IApplicationDbContext _context;

    public GetRefereeLegViewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RefereeLegViewResponse> Handle(
        GetRefereeLegViewQuery request,
        CancellationToken cancellationToken)
    {
        var legNumber = request.LegIndex + 1;

        var race = await _context.Races
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        var isAssignedReferee =
            request.CurrentUserId == race.Referee1Id ||
            request.CurrentUserId == race.Referee2Id;

        if (!isAssignedReferee)
            throw new UnauthorizedAccessException(
                "Only an assigned referee can view the leg view.");

        var leg = await _context.Legs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.RaceId == request.RaceId && l.LegNumber == legNumber,
                cancellationToken)
            ?? throw new KeyNotFoundException("Leg not found.");

        var entries = await _context.Entries
            .AsNoTracking()
            .Include(e => e.Horse)
            .Include(e => e.Jockey)
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .OrderBy(e => e.GateNumber)
            .Select(e => new RefereeLegEntryDto(
                e.EntryId,
                e.GateNumber,
                e.HorseId,
                e.Horse!.Name,
                e.Jockey!.FullName))
            .ToListAsync(cancellationToken);

        var refereeEntries = await _context.LegRefereeEntries
            .AsNoTracking()
            .Where(x => x.RaceId == request.RaceId && x.LegNumber == legNumber)
            .ToListAsync(cancellationToken);

        var opponentId = request.CurrentUserId == race.Referee1Id
            ? race.Referee2Id
            : race.Referee1Id;

        var mine = refereeEntries
            .Where(x => x.RefereeUserId == request.CurrentUserId)
            .ToList();

        var mySubmitted = mine.Count > 0;
        var opponentSubmitted = opponentId != null &&
            refereeEntries.Any(x => x.RefereeUserId == opponentId);
        var bothSubmitted = mySubmitted && opponentSubmitted;

        // Chỉ tiết lộ trạng thái khớp/lệch khi cả hai đã submit.
        string? legStatus = bothSubmitted
            ? (leg.Status == RaceExecutionConstants.LegConfirmed ? "Matched"
               : leg.Status == RaceExecutionConstants.LegConflicted ? "Conflicted"
               : leg.Status)
            : null;

        var mySubmittedData = mySubmitted
            ? mine.Select(x => new RefereeSubmittedItemDto(
                    x.EntryId,
                    RaceExecutionConstants.EncodePosition(x.FinishPosition, x.ResultStatus)))
                .ToList()
            : null;

        // Nháp đã lưu của tôi (nếu chưa submit) — để FE khôi phục khi quay lại.
        List<RefereeSubmittedItemDto>? myDraftData = null;
        if (!mySubmitted)
        {
            myDraftData = await _context.LegRefereeDrafts
                .AsNoTracking()
                .Where(d => d.RaceId == request.RaceId &&
                            d.LegNumber == legNumber &&
                            d.RefereeUserId == request.CurrentUserId)
                .Select(d => new RefereeSubmittedItemDto(d.EntryId, d.Position))
                .ToListAsync(cancellationToken);
            if (myDraftData.Count == 0) myDraftData = null;
        }

        return new RefereeLegViewResponse(
            request.RaceId,
            request.LegIndex,
            legNumber,
            entries,
            mySubmittedData,
            myDraftData,
            mySubmitted,
            opponentSubmitted,
            bothSubmitted,
            legStatus);
    }
}
