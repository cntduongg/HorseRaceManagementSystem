using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// GET /api/races/{raceId}/odds-board — bảng odds cho Admin XEM (Flow 3+7). READ-ONLY.
// Odds là một con số tĩnh, máy tính lúc đóng đăng ký; không ai sửa được. Board tồn tại để
// Admin đối chiếu cơ sở tính toán (số lần về nhất / tổng số lần đua) và xem cược đang đổ vào đâu.
public sealed record GetRaceOddsBoardQuery(int RaceId) : IRequest<RaceOddsBoardResponse>;

public sealed record RaceOddsBoardResponse(
    int RaceId,
    string RaceName,
    string RaceStatus,
    DateTime? RegistrationCloseAt,
    DateTime? OddsComputedAt,
    List<RaceOddsBoardEntryResponse> Entries);

public sealed record RaceOddsBoardEntryResponse(
    int EntryId,
    int HorseId,
    string? HorseName,
    int JockeyId,
    string? JockeyName,
    int? GateNumber,
    // Odds duy nhất — cũng chính là giá spectator cược và khóa vào Prediction.
    decimal Odds,
    // Cơ sở máy dùng để ra con số trên.
    int CareerFirsts,
    int CareerRaces,
    // Tổng điểm spectator đã đặt vào entry này — số liệu tham khảo, KHÔNG ảnh hưởng giá.
    decimal BetPool,
    int BetCount);

public sealed class GetRaceOddsBoardQueryHandler
    : IRequestHandler<GetRaceOddsBoardQuery, RaceOddsBoardResponse>
{
    private readonly IApplicationDbContext _context;

    public GetRaceOddsBoardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RaceOddsBoardResponse> Handle(
        GetRaceOddsBoardQuery request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .AsNoTracking()
            .Where(r => r.RaceId == request.RaceId)
            .Select(r => new
            {
                r.RaceId,
                r.Name,
                r.Status,
                r.RegistrationCloseAt,
                r.OddsComputedAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        var entries = await _context.Entries
            .AsNoTracking()
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .OrderBy(e => e.GateNumber ?? int.MaxValue)
            .ThenBy(e => e.EntryId)
            .Select(e => new
            {
                e.EntryId,
                e.HorseId,
                HorseName = e.Horse.Name,
                e.JockeyId,
                JockeyName = e.Jockey.FullName,
                e.GateNumber,
                e.Odds
            })
            .ToListAsync(cancellationToken);

        var horseIds = entries.Select(e => e.HorseId).Distinct().ToList();

        // Cùng nguồn dữ liệu mà RaceOddsAssigner dùng để tính odds — hiện ra để Admin đối
        // chiếu, không tính lại gì.
        var history = await _context.RaceResults
            .AsNoTracking()
            .Where(r => horseIds.Contains(r.Entry.HorseId) && r.FinalPosition != null)
            .Select(r => new { r.Entry.HorseId, r.FinalPosition })
            .ToListAsync(cancellationToken);

        var pools = await _context.Predictions
            .AsNoTracking()
            .Where(p => p.RaceId == request.RaceId && p.Status != PredictionStatus.Cancelled)
            .GroupBy(p => p.FirstEntryId)
            .Select(g => new { EntryId = g.Key, Amount = g.Sum(x => x.BetAmount), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var poolByEntry = pools.ToDictionary(x => x.EntryId);

        var rows = entries
            .Select(e =>
            {
                var horseHistory = history.Where(h => h.HorseId == e.HorseId).ToList();
                poolByEntry.TryGetValue(e.EntryId, out var pool);

                return new RaceOddsBoardEntryResponse(
                    e.EntryId,
                    e.HorseId,
                    e.HorseName,
                    e.JockeyId,
                    e.JockeyName,
                    e.GateNumber,
                    e.Odds,
                    horseHistory.Count(h => h.FinalPosition == 1),
                    horseHistory.Count,
                    pool?.Amount ?? 0m,
                    pool?.Count ?? 0);
            })
            .ToList();

        return new RaceOddsBoardResponse(
            race.RaceId,
            race.Name,
            race.Status,
            race.RegistrationCloseAt,
            race.OddsComputedAt,
            rows);
    }
}
