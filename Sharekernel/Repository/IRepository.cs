using System.Linq.Expressions;
using ShareKernel.Entity;

namespace ShareKernel.Repository;

public interface IRepository<TEntity, TId> where TEntity : Entity<TId> where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id);
    IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
    Task<TId> AddAsync(TEntity entity);
    Task<TId> UpdateAsync(TEntity entity);
    Task<bool> DeleteAsync(TEntity entity);
}