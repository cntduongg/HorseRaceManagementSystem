using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Horses.GetHorseStatistics;

public sealed class GetHorseStatisticsQueryHandler : IRequestHandler<GetHorseStatisticsQuery, HorseStatisticsResponse?>
{
    private readonly IApplicationDbContext _context;
    public GetHorseStatisticsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<HorseStatisticsResponse?> Handle(GetHorseStatisticsQuery request, CancellationToken cancellationToken)
    {
        // Lấy thông tin ngựa (bao gồm Stamina hiện tại)
        var horse = await _context.Horses.AsNoTracking()
            .FirstOrDefaultAsync(h => h.HorseId == request.HorseId, cancellationToken);
        
        if (horse == null) return null;
        // Truy vấn tất cả kết quả trận đua đã công bố (FinalPosition != null) của ngựa này
        var results = await _context.RaceResults
            .AsNoTracking()
            .Where(rr => rr.Entry.HorseId == request.HorseId && rr.FinalPosition != null)
            .OrderByDescending(rr => rr.PublishedAt)
            .Select(rr => new {
                rr.FinalPosition,
                RaceName = rr.Race.Name,
                PublishedAt = rr.PublishedAt
            })
            .ToListAsync(cancellationToken);
        int totalRaces = results.Count;
        int totalWins = results.Count(r => r.FinalPosition == 1);
        int totalTop3 = results.Count(r => r.FinalPosition <= 3);
        decimal winRate = totalRaces > 0 ? Math.Round(((decimal)totalWins / totalRaces) * 100, 2) : 0m;
        // Trận đấu gần đây nhất
        var recentRace = results.FirstOrDefault();
        int? recentRacePosition = recentRace?.FinalPosition;
        string? recentRaceName = recentRace?.RaceName;
        // Phong độ 5 trận gần nhất
        var recentForm = results.Take(5).Select(r => r.FinalPosition ?? 0).ToList();
        return new HorseStatisticsResponse(
            horse.HorseId,
            horse.Name,
            horse.Stamina,
            horse.HealthStatus,
            totalRaces,
            totalWins,
            totalTop3,
            winRate,
            recentRacePosition,
            recentRaceName,
            recentForm
        );
    }
}