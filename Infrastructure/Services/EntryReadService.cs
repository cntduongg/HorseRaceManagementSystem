using Application.Common;
using Application.Usecases.Entries.GetEntryList;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class EntryReadService : IEntryReadService
{
    private readonly ApplicationDbContext _db;

    public EntryReadService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<EntryListItemResponse>> GetListAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Entries
            .AsNoTracking()
            .OrderBy(x => x.RaceId)
            .ThenBy(x => x.GateNumber ?? int.MaxValue)
            .ThenBy(x => x.EntryId)
            .Select(x => new EntryListItemResponse(
                x.EntryId,
                x.RaceId,
                x.HorseId,
                x.Horse.Name,
                x.Horse.ImageUrl,
                x.JockeyId,
                x.Jockey.FullName,
                x.Jockey.AvatarUrl,
                x.HorseOwnerId,
                x.HorseOwner.FullName,
                x.GateNumber,
                x.Status,
                x.SubmittedAt,
                x.ApprovedAt,
                null,
                x.Race.Tournament != null
    ? x.Race.Tournament.Name
    : null
            ))
            .ToListAsync(cancellationToken);
    }
}