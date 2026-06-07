namespace ShareKernel.Aggregate;

public interface IDomainEventHandler<in TEvent> where TEvent : DomainEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}