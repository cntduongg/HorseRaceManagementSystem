using Application.Common;
using Domain.Aggregates.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class HorseRepository : IHorseRepository
{
    private readonly ApplicationDbContext _context;

    public HorseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Horse?> GetByIdAsync(int horseId, CancellationToken cancellationToken = default)
    {
        return _context.Horses
            .FirstOrDefaultAsync(h => h.HorseId == horseId, cancellationToken);
    }

    public async Task<List<Horse>> GetAsync(
        int? ownerId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Horses.AsNoTracking().AsQueryable();

        if (ownerId is not null)
        {
            query = query.Where(h => h.OwnerId == ownerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(h => h.Status == status);
        }

        return await query
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Horse horse, CancellationToken cancellationToken = default)
    {
        await _context.Horses.AddAsync(horse, cancellationToken);
    }

    public void Remove(Horse horse)
    {
        _context.Horses.Remove(horse);
    }
}
