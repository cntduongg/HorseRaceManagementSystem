using Application.Common;
using Application.Usecases.Tournaments.GetTournamentList;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class TournamentReadService : ITournamentReadService
{
    private readonly ApplicationDbContext _db;

    public TournamentReadService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<TournamentListItemResponse>> GetListAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Tournaments
            .AsNoTracking()
            .OrderByDescending(x => x.StartDate)
            .Select(x => new TournamentListItemResponse(
                x.TournamentId,
                x.Name,
                x.Location,
                x.StartDate,
                x.EndDate,
                x.Status,
                x.Races.Count
            ))
            .ToListAsync(cancellationToken);
    }
}