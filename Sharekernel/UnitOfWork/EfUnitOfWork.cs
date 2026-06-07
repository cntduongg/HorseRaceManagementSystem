

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShareKernel.Aggregate;

namespace ShareKernel.UnitOfWork;

public class EfUnitOfWork<TContext>(TContext context, IDomainEventPublisher eventPublisher) : IUnitOfWork where TContext : DbContext
{
    private IDbContextTransaction? _currentTransaction;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(x => x.Entity)
            .ToList();

        var events = aggregates.SelectMany(x => x.Events).ToList();

        var rowsAffected = await context.SaveChangesAsync(cancellationToken);

        if (events.Count != 0)
        {
            await eventPublisher.PublishAsync(events, cancellationToken);
        }

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearEvents();
        }
        
        return rowsAffected;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("Transaction already started");
        }

        _currentTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction started");
        }

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        context.Dispose();
    }
}