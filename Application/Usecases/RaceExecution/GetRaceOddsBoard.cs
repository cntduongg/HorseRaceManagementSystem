using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// GET /api/races/{raceId}/odds-board — bảng odds cho Admin (modal điều chỉnh odds, Flow 3+7).
// Trả CẢ HAI giá: đề xuất (máy tính) và công bố (spectator sẽ thấy), kèm cơ sở tính toán
// (số lần về nhất / tổng số lần đua) để Admin biết vì sao máy đề xuất con số đó.
public sealed record GetRaceOddsBoardQuery(int RaceId) : IRequest<RaceOddsBoardResponse>;

public sealed record RaceOddsBoardResponse(
    int RaceId,
    string RaceName,
    string RaceStatus,
    DateTime? RegistrationCloseAt,
    DateTime? OddsComputedAt,
    DateTime? OddsPublishedAt,
    DateTime? BettingLockedAt,
    decimal HouseMarginPercent,
    decimal MinOdds,
    decimal MaxOdds,
    bool CanEdit,
    List<RaceOddsBoardEntryResponse> Entries);

public sealed record RaceOddsBoardEntryResponse(
    int EntryId,
    int HorseId,
    string? HorseName,
    int JockeyId,
    string? JockeyName,
    int? GateNumber,
    // Giá máy đề xuất từ lịch sử thắng — chỉ Admin thấy.
    decimal SuggestedOdds,
    // Giá spectator đặt cược. Mặc định = đề xuất − 10%, Admin sửa được.
    decimal PublishedOdds,
    // Giá công bố hệ thống đề nghị cho giá đề xuất hiện tại (để Admin bấm "reset").
    decimal RecommendedPublishedOdds,
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
                r.OddsComputedAt,
                r.OddsPublishedAt,
                r.BettingLockedAt
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
                e.Odds,
                e.PublishedOdds
            })
            .ToListAsync(cancellationToken);

        var horseIds = entries.Select(e => e.HorseId).Distinct().ToList();

        // Cùng nguồn dữ liệu mà CloseRegistration dùng để tính odds đề xuất — hiện ra để Admin
        // đối chiếu, không tính lại gì.
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
                    e.PublishedOdds,
                    RaceOddsPolicy.PublishedFromSuggested(e.Odds),
                    horseHistory.Count(h => h.FinalPosition == 1),
                    horseHistory.Count,
                    pool?.Amount ?? 0m,
                    pool?.Count ?? 0);
            })
            .ToList();

        // Sửa odds được tới khi khóa cược — sau đó giá phải đứng yên vì cược đã chốt theo nó.
        var canEdit = race.Status == RaceExecutionConstants.RaceScheduled
            && race.OddsComputedAt != null
            && race.BettingLockedAt == null;

        return new RaceOddsBoardResponse(
            race.RaceId,
            race.Name,
            race.Status,
            race.RegistrationCloseAt,
            race.OddsComputedAt,
            race.OddsPublishedAt,
            race.BettingLockedAt,
            RaceOddsPolicy.HouseMarginRate * 100m,
            RaceOddsPolicy.MinOdds,
            RaceOddsPolicy.MaxOdds,
            canEdit,
            rows);
    }
}
