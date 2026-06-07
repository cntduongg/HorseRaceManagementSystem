using System.Linq.Expressions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ShareKernel.Entity;
using ShareKernel.Repository;

namespace Infrastructure.Repositories;

public class EfRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public EfRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(TId id)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Id.Equals(id));
    }

    public IQueryable<TEntity> GetAll()
    {
        return _dbSet.AsQueryable();
    }

    public IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
    {
        return _dbSet.Where(predicate);
    }

    public async Task<TId> AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);

        return entity.Id;
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public Task<TId> UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);

        return Task.FromResult(entity.Id);
    }

    public Task UpdateRangeAsync(IEnumerable<TEntity> entities)
    {
        _dbSet.UpdateRange(entities);

        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(TEntity entity)
    {
        _dbSet.Remove(entity);

        return Task.FromResult(true);
    }

    public Task DeleteRangeAsync(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);

        return Task.CompletedTask;
    }
}