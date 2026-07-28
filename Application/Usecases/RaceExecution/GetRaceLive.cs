using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// GET /api/races/{raceId}/live — snapshot theo dõi trực tiếp cho Spectator.
// Cũng chính là payload được SignalR đẩy qua event "RaceLiveChanged" (xem IRaceLiveNotifier),
// nên FE chỉ cần một reducer duy nhất cho cả GET lẫn push.
//
// Blind Double-Entry (Flow 4): payload KHÔNG chứa trạng thái submit của từng referee, và
// Results chỉ có ở leg đã Confirmed/Resolved (nguồn: LegOfficialResults).
public sealed record GetRaceLiveQuery(int RaceId) : IQuery<RaceLiveResponse>;

// Đủ dữ liệu trình bày để FE dựng đường đua (màu lông, số áo theo GateNumber, odds) mà không
// phải gọi thêm GET /api/predictions/races/{id}/legs/{n}/odds.
public sealed record RaceLiveEntryDto(
    int EntryId,
    int HorseId,
    int? GateNumber,
    string HorseName,
    string JockeyName,
    string? Color,        // màu lông (free-text: Bay/Chestnut/…)
    string? ImageUrl,
    decimal Odds);        // đã khóa lúc đóng đăng ký

public sealed record RaceLiveLegResultDto(
    int EntryId,
    int Position, // >0 = thứ hạng; -1 = DNF; -2 = DQ (RaceExecutionConstants.EncodePosition)
    decimal Points);

public sealed record RaceLiveLegDto(
    int LegIndex,
    int LegNumber,
    string Status,            // Pending|AwaitingSecondReferee|Confirmed|Conflicted|Resolved (blind)
    bool IsConfirmed,         // Confirmed || Resolved → FE hiện bảng vị trí
    bool IsConflicted,        // Conflicted → FE hiện "Đang xem xét"
    string? ConfirmationType, // AutoMatched | AdminOverride | null
    DateTime? StartedAt,      // mốc leg bắt đầu chạy — FE đếm giờ khi đang đua
    DateTime? FinishedAt,
    DateTime? ConfirmedAt,    // cùng StartedAt cho phép mọi client đồng bộ pha khi phát lại
    IReadOnlyList<RaceLiveLegResultDto> Results); // rỗng nếu leg chưa Confirmed/Resolved

public sealed record RaceLiveStandingDto(
    int EntryId,
    int? GateNumber,
    string HorseName,
    string JockeyName,
    decimal TotalPoints,
    int LegWins,
    int Leg2nds,
    int LegTop3,
    bool IsDq,
    int Position);

public sealed record RaceLiveResponse(
    int RaceId,
    string RaceName,
    int TournamentId,
    string TournamentName,
    string Status, // Scheduled|InProgress|Paused|PendingResult|Finished|Cancelled
    DateTime ScheduledStartTime,
    int TotalLegs,
    int CurrentLegIndex,
    int ConfirmedLegCount,
    IReadOnlyList<RaceLiveEntryDto> Entries,
    IReadOnlyList<RaceLiveLegDto> Legs,
    IReadOnlyList<RaceLiveStandingDto> Standings,
    DateTime SnapshotAt);

public sealed class GetRaceLiveQueryHandler
    : IRequestHandler<GetRaceLiveQuery, RaceLiveResponse>
{
    private readonly IApplicationDbContext _context;

    public GetRaceLiveQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RaceLiveResponse> Handle(
        GetRaceLiveQuery request,
        CancellationToken cancellationToken)
    {
        // KHÔNG Include(l => l.RefereeEntries): không nạp dữ liệu blind chính là bảo đảm
        // cấu trúc để nó không thể lọt ra payload công khai.
        var race = await _context.Races
            .AsNoTracking()
            .Include(r => r.Tournament)
            .Include(r => r.Legs).ThenInclude(l => l.OfficialResults)
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        var entries = await _context.Entries
            .AsNoTracking()
            .Include(e => e.Horse)
            .Include(e => e.Jockey)
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .ToListAsync(cancellationToken);

        var entryDtos = entries
            .OrderBy(e => e.GateNumber ?? int.MaxValue)
            .ThenBy(e => e.EntryId)
            .Select(e => new RaceLiveEntryDto(
                e.EntryId,
                e.HorseId,
                e.GateNumber,
                e.Horse?.Name ?? $"Horse #{e.HorseId}",
                e.Jockey?.FullName ?? $"Jockey #{e.JockeyId}",
                e.Horse?.Color,
                e.Horse?.ImageUrl,
                e.Odds))
            .ToList();

        var legs = race.Legs
            .OrderBy(l => l.LegNumber)
            .Select(l =>
            {
                var isConfirmed = l.Status is RaceExecutionConstants.LegConfirmed
                                           or RaceExecutionConstants.LegResolved;

                // Phòng thủ nhiều lớp: LegOfficialResults chỉ tồn tại sau khi chốt, nhưng
                // vẫn chặn thêm một lần theo status để không bao giờ lộ vị trí chưa xác nhận.
                var results = isConfirmed
                    ? l.OfficialResults
                        .Select(o => new RaceLiveLegResultDto(
                            o.EntryId,
                            RaceExecutionConstants.EncodePosition(o.FinishPosition, o.ResultStatus),
                            o.LegPoints))
                        .OrderBy(r => r.Position > 0 ? r.Position : int.MaxValue) // DNF/DQ xuống cuối
                        .ThenBy(r => r.EntryId)
                        .ToList()
                    : new List<RaceLiveLegResultDto>();

                return new RaceLiveLegDto(
                    LegIndex: l.LegNumber - 1,
                    LegNumber: l.LegNumber,
                    Status: l.Status,
                    IsConfirmed: isConfirmed,
                    IsConflicted: l.Status == RaceExecutionConstants.LegConflicted,
                    ConfirmationType: l.ConfirmationType,
                    StartedAt: l.StartedAt,
                    FinishedAt: l.FinishedAt,
                    ConfirmedAt: l.ConfirmedAt,
                    Results: results);
            })
            .ToList();

        // Giữ đồng nhất với GetRaceExecution để hai endpoint không bao giờ lệch nhau.
        var current = legs.FindIndex(l =>
            l.Status != RaceExecutionConstants.LegConfirmed &&
            l.Status != RaceExecutionConstants.LegResolved);
        if (current < 0) current = Math.Max(0, legs.Count - 1);

        // Chỉ lấy kết quả của leg ĐÃ chốt — cùng lớp phòng thủ với biến `results` ở trên, để vị
        // trí chưa xác nhận không bao giờ rò ra qua đường bảng điểm.
        var confirmedOfficials = race.Legs
            .Where(l => l.Status is RaceExecutionConstants.LegConfirmed
                                 or RaceExecutionConstants.LegResolved)
            .SelectMany(l => l.OfficialResults)
            .ToList();

        var dqSet = (await _context.Violations
            .AsNoTracking()
            .Where(v => v.RaceId == request.RaceId && v.Status == "Approved" && v.Penalty == "DQ")
            .Select(v => v.EntryId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var standings = BuildStandings(entries, confirmedOfficials, dqSet);

        return new RaceLiveResponse(
            race.RaceId,
            race.Name,
            race.TournamentId,
            race.Tournament?.Name ?? $"Tournament #{race.TournamentId}",
            race.Status,
            race.ScheduledStartTime,
            race.NumberOfLegs,
            current,
            legs.Count(l => l.IsConfirmed),
            entryDtos,
            legs,
            standings,
            DateTime.UtcNow);
    }

    // Dùng CHUNG RaceRankingCalculator với publish / publication-review / GetRaceStandings.
    // Trước đây hàm này tự sắp lấy (TotalPoints → LegWins → LegTop3) và KHÔNG biết tới Race DQ,
    // nên khi hòa điểm, bảng Spectator đang xem xếp khác hẳn kết quả sắp được công bố — đúng
    // triệu chứng "admin thấy Lucario #1 còn spectator thấy Groudon #1".
    // Vẫn là bảng TẠM TÍNH (chỉ gồm leg đã chốt) — FE gắn nhãn đúng như vậy.
    private static List<RaceLiveStandingDto> BuildStandings(
        List<Entry> entries,
        List<LegOfficialResult> confirmedOfficials,
        ISet<int> dqEntryIds)
    {
        return RaceRankingCalculator.Rank(entries, confirmedOfficials, dqEntryIds)
            .Select(r => new RaceLiveStandingDto(
                r.Entry.EntryId,
                r.Entry.GateNumber,
                r.Entry.Horse?.Name ?? $"Horse #{r.Entry.HorseId}",
                r.Entry.Jockey?.FullName ?? $"Jockey #{r.Entry.JockeyId}",
                TotalPoints: r.TotalPoints,
                LegWins: r.LegWins,
                Leg2nds: r.Leg2nds,
                LegTop3: r.LegTop3,
                IsDq: r.IsDq,
                Position: r.FinalPosition))
            .ToList();
    }
}
