
using System.ComponentModel.DataAnnotations.Schema;
using ShareKernel.Entity;
namespace ShareKernel.Aggregate;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot where TId : notnull
{
    [NotMapped]
    private readonly List<DomainEvent> _events = [];
    
    [NotMapped]
    public IReadOnlyList<DomainEvent> Events => _events.AsReadOnly();

    protected void RaiseEvent(DomainEvent @event)
    {
        _events.Add(@event);
    }

    public void ClearEvents()
    {
        _events.Clear();
    }
}