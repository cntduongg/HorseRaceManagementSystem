using Application.Common;
using Domain.Aggregates.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class EntryRepository : IEntryRepository
{
    private readonly ApplicationDbContext _context;

    public EntryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Entry>> GetByHorseAndStatusAsync(
        int horseId,
        string status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Entries
            .Where(e => e.HorseId == horseId && e.Status == status)
            .ToListAsync(cancellationToken);
    }
}
