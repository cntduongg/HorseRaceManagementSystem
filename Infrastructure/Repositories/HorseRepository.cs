using Application.Common;
using Domain.Aggregates.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class HorseRepository : IHorseRepository
{
	private readonly ApplicationDbContext _context;

	public HorseRepository(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<Horse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
	{
		return await _context.Horses
			.Include(h => h.Entries)
			.FirstOrDefaultAsync(h => h.HorseId == id, cancellationToken);
	}

	public async Task<List<Horse>> GetPendingAsync(CancellationToken cancellationToken = default)
	{
		return await _context.Horses
			.Include(h => h.Owner)
			.Where(h => h.Status == "Pending")
			.OrderBy(h => h.CreatedAt)
			.ToListAsync(cancellationToken);
	}

	public Task UpdateAsync(Horse horse, CancellationToken cancellationToken = default)
	{
		_context.Horses.Update(horse);
		return Task.CompletedTask;
	}
}