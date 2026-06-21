using Application.Common;
using Application.Usecases.RaceResults.GetRaceResultDetail;
using Application.Usecases.RaceResults.GetRaceResultList;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class RaceResultReadService : IRaceResultReadService
{
    private readonly ApplicationDbContext _db;

    public RaceResultReadService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<RaceResultListItemResponse>> GetListAsync(
        CancellationToken cancellationToken)
    {
        return await _db.RaceResults
            .AsNoTracking()
            .OrderBy(x => x.RaceId)
            .ThenBy(x => x.FinalPosition ?? int.MaxValue)
            .ThenByDescending(x => x.TotalPoints)
            .Select(x => new RaceResultListItemResponse(
                x.RaceId,
                x.EntryId,
                x.Entry.Horse.Name,
                x.Entry.Horse.Owner.FullName,
                x.Entry.Jockey.FullName,
                x.FinalPosition,
                x.TotalPoints
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<RaceResultDetailResponse?> GetDetailAsync(
        int raceId,
        int entryId,
        CancellationToken cancellationToken)
    {
        return await _db.RaceResults
            .AsNoTracking()
            .Where(x => x.RaceId == raceId && x.EntryId == entryId)
            .Select(x => new RaceResultDetailResponse(
                x.RaceId,
                x.EntryId,
                x.Entry.Horse.Name,
                x.Entry.Horse.Owner.FullName,
                x.Entry.Jockey.FullName,
                x.TotalPoints,
                x.FinalPosition,
                x.IsRaceDQ,
                x.LegWinCount,
                x.LegTop3Count,
                x.PublishedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}