using Application.Common;
using Domain.Aggregates.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EntryRepository : IEntryRepository
{
    private readonly ApplicationDbContext _context;

    public EntryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Entry>> GetByHorseIdAsync(int horseId, CancellationToken cancellationToken = default)
    {
        return await _context.Entries
            .Where(e => e.HorseId == horseId)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateRangeAsync(IEnumerable<Entry> entries, CancellationToken cancellationToken = default)
    {
        _context.Entries.UpdateRange(entries);
        return Task.CompletedTask;
    }
}