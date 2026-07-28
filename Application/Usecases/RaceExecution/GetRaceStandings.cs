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
    decimal TotalPoints,
    int LegWins,
    int LegTop3,
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

        var standings = entries.Select(e =>
        {
            var rows = officials.Where(o => o.EntryId == e.EntryId).ToList();
            var totalPoints = rows.Sum(r => r.LegPoints);
            var legWins = rows.Count(r =>
                r.ResultStatus == RaceExecutionConstants.ResultFinished && r.FinishPosition == 1);
            var legTop3 = rows.Count(r =>
                r.ResultStatus == RaceExecutionConstants.ResultFinished &&
                r.FinishPosition is >= 1 and <= 3);

            return new RaceStandingDto(
                e.EntryId,
                e.GateNumber,
                e.Horse?.Name ?? $"Horse #{e.HorseId}",
                e.Jockey?.FullName ?? $"Jockey #{e.JockeyId}",
                totalPoints,
                legWins,
                legTop3,
                Position: null);
        })
        .OrderByDescending(s => s.TotalPoints)
        .ThenByDescending(s => s.LegWins)
        .ThenByDescending(s => s.LegTop3)
        .ToList();

        // Gán vị trí tạm theo thứ tự đã sắp.
        return standings
            .Select((s, i) => s with { Position = i + 1 })
            .ToList();
    }
}
