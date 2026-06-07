

namespace ShareKernel.Aggregate;
public interface IAggregateRoot
{
    public IReadOnlyList<DomainEvent> Events { get; }
    public void ClearEvents();
}