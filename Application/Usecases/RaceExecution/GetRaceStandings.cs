using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// GET /api/races/{raceId}/standings — bảng điểm live (tổng Leg Points từ kết quả chính thức).
public sealed record GetRaceStandingsQuery(int RaceId) : IQuery<List<RaceStandingDto>>;

public sealed record RaceStandingDto(
    int EntryId,
    int? GateNumber,
    string HorseName,
    string JockeyName,
    int TotalPoints,
    int LegWins,
    int Leg2nds,
    int LegTop3,
    bool IsDq,
    int? Position);

public sealed class GetRaceStandingsQueryHandler
    : IRequestHandler<GetRaceStandingsQuery, List<RaceStandingDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRaceStandingsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RaceStandingDto>> Handle(
        GetRaceStandingsQuery request,
        CancellationToken cancellationToken)
    {
        var entries = await _context.Entries
            .AsNoTracking()
            .Include(e => e.Horse)
            .Include(e => e.Jockey)
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .ToListAsync(cancellationToken);

        var officials = await _context.LegOfficialResults
            .AsNoTracking()
            .Where(o => o.RaceId == request.RaceId)
            .ToListAsync(cancellationToken);

        // Entry bị Race DQ (vi phạm đã duyệt, Penalty = DQ) — phải biết ở đây, nếu không bảng
        // này xếp một kiểu còn kết quả publish lại xếp kiểu khác.
        var dqSet = (await _context.Violations
            .AsNoTracking()
            .Where(v => v.RaceId == request.RaceId && v.Status == "Approved" && v.Penalty == "DQ")
            .Select(v => v.EntryId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        // Dùng CHUNG RaceRankingCalculator với publish & publication-review.
        // Trước đây chỗ này tự sắp lấy (TotalPoints → LegWins → LegTop3) trong khi publish dùng
        // (TotalPoints → LegWins → Leg2nds → vị trí leg cuối → EntryId) và có xử lý DQ ⇒ hễ hòa
        // điểm là Admin nhìn thấy một thứ tự, kết quả công bố ra một thứ tự khác.
        var ranked = RaceRankingCalculator.Rank(entries, officials, dqSet);

        return ranked
            .Select(r => new RaceStandingDto(
                r.Entry.EntryId,
                r.Entry.GateNumber,
                r.Entry.Horse?.Name ?? $"Horse #{r.Entry.HorseId}",
                r.Entry.Jockey?.FullName ?? $"Jockey #{r.Entry.JockeyId}",
                r.TotalPoints,
                r.LegWins,
                r.Leg2nds,
                r.LegTop3,
                r.IsDq,
                Position: r.FinalPosition))
            .ToList();
    }
}
