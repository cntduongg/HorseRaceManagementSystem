using Application.Common;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReviewHistoryRepository : IReviewHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public ReviewHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ReviewHistory history,
        CancellationToken cancellationToken = default)
    {
        await _context.ReviewHistories.AddAsync(history, cancellationToken);
    }

    public async Task<List<ReviewHistory>> GetAsync(
        ReviewEntity? entity,
        int? entityId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReviewHistories
            .Include(x => x.Admin)
            .AsQueryable();

        if (entity.HasValue)
            query = query.Where(x => x.EntityType == entity.Value);

        if (entityId.HasValue)
            query = query.Where(x => x.EntityId == entityId.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}